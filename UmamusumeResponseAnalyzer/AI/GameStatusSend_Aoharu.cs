using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.Game.TurnInfo;
using UmamusumeResponseAnalyzer.Game;
using Spectre.Console;
using UmamusumeResponseAnalyzer.Communications.Subscriptions;
using MathNet.Numerics.RootFinding;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;

namespace UmamusumeResponseAnalyzer.AI
{
    /// <summary>
    /// 传递给AI的人头信息的基本接口（测试）
    /// </summary>
    public class AoharuPerson:UATPerson
    {
        //0代表未加载（例如前两个回合的npc），1代表友人（R或SSR都行），2代表普通支援卡，3代表npc人头，4理事长，5记者，6桐生院葵，7不带卡的友人。暂不支持其他友人/团队卡
        //public new int personType = 0;
        public int member_state;
        public int soul_threshold_id;
        public int soul_event_state;
        public bool isGuide;
        public bool isSoul;
        public AoharuPerson()
        {
            member_state = 0;
            soul_threshold_id = 0;
            soul_event_state = 0;
            isGuide = false;
            isSoul = false;
        }
    }
    public class GameStatusSend_Aoharu<T>:GameStatusSend_UAT
    where T: AoharuPerson, new()
    {
        //public int umaId;//马娘编号，见KnownUmas.cpp
        public int umaStar;//几星
        public bool islegal;//是否为有效的回合数据

        //public int turn;//回合数，从0开始，到77结束
        //public int vital;//体力，叫做“vital”是因为游戏里就这样叫的
        //public int maxVital;//体力上限
        //public int motivation;//干劲，从1到5分别是绝不调到绝好调

        //public int[] fiveStatus;//五维属性，1200以上不减半
        //public int[] fiveStatusLimit;//五维属性上限，1200以上不减半
        //public int skillPt;//技能点
        //public int skillScore;//已买技能的分数
        //public int[] trainLevelCount;

        public double ptScoreRate;
        //public int failureRateBias;//失败率改变量。练习上手=2，练习下手=-2

        //public bool isQieZhe;//切者
        //public bool isAiJiao;//爱娇
        //public bool isPositiveThinking;//ポジティブ思考，友人第三段出行选上的buff，可以防一次掉心情
        public bool isRefreshMind;//休息的心得,每回合体力+5

        public int[] zhongMaBlueCount;//种马的蓝因子个数，假设只有3星
        public int[] zhongMaExtraBonus;//种马的剧本因子以及技能白因子（等效成pt），每次继承加多少。全大师杯因子典型值大约是30速30力200pt

        public int saihou;
        public bool isRacing;
        //public int[] cardId;
        public new T[] persons;//依次是可青春训练人头（先是支援卡（顺序随意）：0~4或5，再是npc），理事长20，记者21，桐生院22，理子23（带没带卡都是23）



        /// <summary>
        /// 游戏状态，不为1时，为重复回合
        /// </summary>
        public int playing_state;

        public bool isRepeatTurn()
        {
            return this.playing_state != 1;
        }

        public GameStatusSend_Aoharu(Gallop.SingleModeCheckEventResponse @event):base(@event)
        {
            islegal = false;
            playing_state = @event.data.chara_info.playing_state;
            //if ((@event.data.unchecked_event_array != null && @event.data.unchecked_event_array.Length > 0)) return;
            if (
                (@event.data.chara_info.playing_state == 1) ||
                (@event.data.chara_info.playing_state == 26 && @event.IsScenario(ScenarioType.Mecha)) 
                )
            {

            }
            else
            {
                //重复显示的回合直接return，就不发了
                //return; //不能不发啊
            }

            //if(@event.data.race_start_info != null)
            // isRacing = true;
            for (var i = 0; i < 5; i++)
            {
                // isRacing &= (@event.data.home_info.command_info_array[i].is_enable == 0);
            }
            
            islegal = true;
            //从游戏json的id到ai的人头编号的换算
            var headIdConvert = new Dictionary<int, int>();
            friend_type = 0;
            persons = new T[24];
            for (var i = 0; i < 24; i++)
                persons[i] = new T();
            normalCardCount = 0;

            {
                for (var i = 0; i < 6; i++)
                {
                    if (cardId[i] / 10 == 30036)//ssr理子
                    {
                        friend_type = 1;
                        persons[23].cardIdInGame = i;
                        headIdConvert[i + 1] = 23;
                    }
                    else if (cardId[i] / 10 == 10060)//r理子
                    {
                        friend_type = 2;
                        persons[23].cardIdInGame = i;
                        headIdConvert[i + 1] = 23;
                    }
                    else
                    {
                        persons[normalCardCount].trainType = Database.Names.GetSupportCard(cardId[i] / 10).Type;
                        persons[normalCardCount].cardIdInGame = i;
                        headIdConvert[i + 1] = normalCardCount;
                        normalCardCount += 1;
                    }
                }
            }
            friend_stage = 0;
            //友人出行用了几次
            if (friend_type != 0)
            {
                var friendJson = @event.data.chara_info.evaluation_info_array.First(x => x.target_id == friend_personId + 1);
                friend_outgoingUsed = friendJson.story_step;
                if (friendJson.is_outing == 1)
                    friend_stage = 2;
                else
                {
                    var friendClicked = false;//友人卡是否点过第一次
                    for (var t = @event.data.chara_info.turn - 1; t >= 1; t--)
                    {
                        if (GameStats.stats[t] == null)
                        {
                            break;
                        }

                        if (!GameGlobal.TrainIds.Any(x => x == GameStats.stats[t].playerChoice)) //没训练
                            continue;
                        if (GameStats.stats[t].isTrainingFailed)//训练失败
                            continue;
                        if (!GameStats.stats[t].aoharu_rikoAtTrain[GameGlobal.ToTrainIndex[GameStats.stats[t].playerChoice]])
                            continue;//没点友人

                        friendClicked = true;
                        break;
                    }
                    friend_stage = friendClicked ? 1 : 0;
                }
            }
            else
            {
                friend_outgoingUsed = 0;
            }

            for (var i = 0; i < normalCardCount; i++)
                persons[i].personType = 2;
            for (var i = normalCardCount; i < 20; i++)
                persons[i].personType = 3;
            persons[20].personType = 4;
            persons[21].personType = 5;
            persons[22].personType = 6;
            persons[23].personType = friend_type == 0 ? 7 : 1;
            headIdConvert[102] = 20;
            headIdConvert[103] = 21;
            headIdConvert[104] = 22;
            if (friend_type == 0)
            {
                headIdConvert[106] = 23;
            }

            if (@event.data.chara_info.turn >= 3)
            {
                var i = normalCardCount;
                foreach (var s in @event.data.team_data_set.evaluation_info_array)
                {
                    //npc当且仅当s.chara_id==s.target_id
                    if (s.chara_id == s.target_id)
                    {
                        headIdConvert[s.chara_id] = i;
                        persons[i].charaId = s.chara_id;
                        i += 1;
                    }
                }
            //if (i != 15)
            //{
            //    throw new Exception("npc人数不正确，可能是因为带了佐岳以外的友人团队卡");
            //}
            }

            //到目前为止，headIdConvert写完了

            //羁绊
            foreach (var s in @event.data.chara_info.evaluation_info_array)
            {
                if (!headIdConvert.ContainsKey(s.target_id))
                    continue;
                var p = headIdConvert[s.target_id];
                persons[p].friendship = s.evaluation;
            }
            //青春杯信息
            if (@event.data.chara_info.turn >= 3)
            {
                for (var i = 0; i < 15; i++)
                {
                    if (!headIdConvert.ContainsValue(i))continue;
                    var chara_id = @event.data.team_data_set.evaluation_info_array.First(x => x.target_id == headIdConvert.First(x => x.Value == i).Key).chara_id;
                    var p = @event.data.team_data_set.evaluation_info_array.First(x => x.chara_id == chara_id);

                    persons[i].member_state = p.member_state;
                    persons[i].soul_threshold_id = p.soul_threshold_id;  //5
                    persons[i].soul_event_state = p.soul_event_state;    //1
                }
            }

            if (@event.data.home_info == null) return;
            personDistribution = new int[5, 5];
            for (var i = 0; i < 5; i++)
                for (var j = 0; j < 5; j++)
                    personDistribution[i, j] = -1;
            var available_command_num = 0;
            foreach (var train in @event.data.home_info.command_info_array)
            {
                if (train.is_enable > 0)
                    available_command_array[available_command_num++] = train.command_id;
                if (!GameGlobal.ToTrainIndex.ContainsKey(train.command_id))//不是正常训练
                    continue;
                var trainId = GameGlobal.ToTrainIndex[train.command_id];

                var j = 0;
                foreach (var p in train.training_partner_array)
                {
                    var pid = headIdConvert[p];
                    personDistribution[trainId, j] = pid;
                    j += 1;
                }
                foreach (var p in train.tips_event_partner_array)
                {
                    var pid = headIdConvert[p];
                    persons[pid].isHint = true;
                }
            }
       
            foreach (var train in @event.data.team_data_set.command_info_array)
            {
                //Console.WriteLine(train.command_id);
                if (!GameGlobal.ToTrainIndex.ContainsKey(train.command_id))//不是正常训练
                    continue;
                //Console.WriteLine("!");
                foreach (var p in train.guide_event_partner_array)
                {
                    var pid = headIdConvert[p];
                    persons[pid].isGuide = true;
                }
                foreach (var p in train.soul_event_partner_array)
                {
                    var pid = headIdConvert[p];
                    persons[pid].isSoul = true;
                }
            }
            var currentVital = @event.data.chara_info.vital;
            //maxVital = @event.data.chara_info.max_vital;
            var currentFiveValue = fiveStatus;

            var trainItems = new Dictionary<int, Gallop.SingleModeCommandInfo>();
            if (@event.IsScenario(ScenarioType.Ura) | @event.IsScenario(ScenarioType.Aoharu))
            {
                //速耐力根智，6xx为合宿时ID
                trainItems.Add(101, @event.data.home_info.command_info_array.Any(x => x.command_id == 601) ? @event.data.home_info.command_info_array.First(x => x.command_id == 601) : @event.data.home_info.command_info_array.First(x => x.command_id == 101));
                trainItems.Add(105, @event.data.home_info.command_info_array.Any(x => x.command_id == 602) ? @event.data.home_info.command_info_array.First(x => x.command_id == 602) : @event.data.home_info.command_info_array.First(x => x.command_id == 105));
                trainItems.Add(102, @event.data.home_info.command_info_array.Any(x => x.command_id == 603) ? @event.data.home_info.command_info_array.First(x => x.command_id == 603) : @event.data.home_info.command_info_array.First(x => x.command_id == 102));
                trainItems.Add(103, @event.data.home_info.command_info_array.Any(x => x.command_id == 604) ? @event.data.home_info.command_info_array.First(x => x.command_id == 604) : @event.data.home_info.command_info_array.First(x => x.command_id == 103));
                trainItems.Add(106, @event.data.home_info.command_info_array.Any(x => x.command_id == 605) ? @event.data.home_info.command_info_array.First(x => x.command_id == 605) : @event.data.home_info.command_info_array.First(x => x.command_id == 106));
            }

            var trainStats = new TrainStats[5];
            for (var t = 0; t < 5; t++)
            {
                var tid = GameGlobal.TrainIds[t];
                var trainParams = new Dictionary<int, int>()
                {
                    {1,0},
                    {2,0},
                    {3,0},
                    {4,0},
                    {5,0},
                    {30,0},
                    {10,0},
                };
                //去掉剧本加成的训练值（游戏里的下层显示）
                foreach (var item in @event.data.home_info.command_info_array)
                {
                    if (GameGlobal.ToTrainId.TryGetValue(item.command_id, out var value) && value == tid)
                    {
                        foreach (var trainParam in item.params_inc_dec_info_array)
                        {
                            trainParams[trainParam.target_type] += trainParam.value;
                        }
                    }
                }

                //剧本加成（上层显示）
                foreach (var item in @event.data.team_data_set.command_info_array)
                {
                    if (GameGlobal.ToTrainId.ContainsKey(item.command_id) &&
                        GameGlobal.ToTrainId[item.command_id] == tid)
                    {
                        foreach (var trainParam in item.params_inc_dec_info_array)
                        {
                            trainParams[trainParam.target_type] += trainParam.value;
                        }
                    }
                }

                var stats = new TrainStats
                {
                    FailureRate = trainItems[tid].failure_rate,
                    VitalGain = trainParams[10]
                };
                if (currentVital + stats.VitalGain > maxVital)
                    stats.VitalGain = maxVital - currentVital;
                if (stats.VitalGain < -currentVital)
                    stats.VitalGain = -currentVital;
                stats.FiveValueGain = [trainParams[1], trainParams[2], trainParams[3], trainParams[4], trainParams[5]];
                for (var i = 0; i < 5; i++)
                    stats.FiveValueGain[i] = ScoreUtils.ReviseOver1200(currentFiveValue[i] + stats.FiveValueGain[i]) - ScoreUtils.ReviseOver1200(currentFiveValue[i]);
                stats.PtGain = trainParams[30];
                for (var i = 0; i < 5; i++)
                    trainValue[t, i] = stats.FiveValueGain[i];
                trainValue[t, 5] = stats.PtGain;
                trainValue[t, 6] = stats.VitalGain;
                failRate[t] = stats.FailureRate;
            }//for
        }
    }
}
