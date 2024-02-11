using Gallop;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.Game;
using UmamusumeResponseAnalyzer.Entities;

namespace UmamusumeResponseAnalyzer.AI
{
    public class GameStatusSend_LArc
    {
        public int umaId;//马娘编号，见KnownUmas.cpp
        //int16_t fiveStatusBonus[5];//马娘的五维属性的成长率
        public int turn;//回合数，从0开始，到77结束
        public int vital;//体力，叫做“vital”是因为游戏里就这样叫的
        public int maxVital;//体力上限
        public bool isQieZhe;//切者
        public bool isAiJiao;//爱娇
        public int failureRateBias;//失败率改变量。练习上手=2，练习下手=-2
        public int[] fiveStatus;//五维属性，1200以上不减半
        public int[] fiveStatusLimit;//五维属性上限，1200以上不减半
        public int skillPt;//技能点
        public int skillScore;//已买技能的分数
        public int motivation;//干劲，从1到5分别是绝不调到绝好调
        public bool isPositiveThinking;//ポジティブ思考，友人第三段出行选上的buff，可以防一次掉心情
        public int[] trainLevelCount;//五个训练的等级的计数，实际训练等级=min(5,t/12+1)
        public int[] zhongMaBlueCount;//种马的蓝因子个数，假设只有3星
        public int[] zhongMaExtraBonus;//种马的剧本因子以及技能白因子（等效成pt），每次继承加多少。全大师杯因子典型值大约是30速30力200pt
        public int normalCardCount;//速耐力根智卡的数量
        public int[] cardId;//6张卡的id
        //SupportCard cardParam[6];//六张卡的参数，拷贝到Game类里，一整局内不变，顺序任意。这样做的目的是训练ai时可能要随机改变卡的参数提高鲁棒性，所以每个game的卡的参数可能不一样
        //int16_t saihou;//赛后加成
        public LArcPerson[] persons;//如果不带其他友人团队卡，最多18个头。依次是15个可充电人头（先是支援卡（顺序随意）：0~4或5，再是npc：5或6~14），理事长15，记者16，佐岳17（带没带卡都是17）
        //bool isRacing;//这个回合是否在比赛

        public int motivationDropCount;//掉过几次心情了，不包括剧本事件（已知同一个掉心情不会出现多次，一共3个掉心情事件，所以之前掉过越多，之后掉的概率越低）


        public bool larc_isAbroad;//这个回合是否在海外
        public int larc_supportPtAll;//所有人（自己+其他人）的支援pt之和，每1700支援pt对应1%期待度
        public int larc_shixingPt;//适性pt
        public int[] larc_levels;//10个海外适性的等级，0为未解锁。顺序是游戏里从左上到右下的顺序，顺序编号在小黑板传过来的时候已经处理好了
        public bool larc_isSSS;//是否为sss
        public int larc_ssWin;//一共多少人头的ss
        public int larc_ssWinSinceLastSSS;//从上次sss到现在win过几次ss（决定了下一个是sss的概率）
        //bool[] larc_allowedDebuffsFirstLarc;//第一次凯旋门可以不消哪些debuff。玩家可以设置，满足则认为可以赢凯旋门

        //public int larc_zuoyueType;//没带佐岳卡=0，带的SSR卡=1，带的R卡=2
        //public double larc_zuoyueVitalBonus;//佐岳卡的回复量倍数（满破1.8）
        //public double larc_zuoyueStatusBonus;//佐岳卡的事件效果倍数（满破1.2）
        public bool larc_zuoyueFirstClick;//佐岳是否点过第一次
        public bool larc_zuoyueOutgoingUnlocked;//佐岳外出解锁
        public bool larc_zuoyueOutgoingRefused;//是否拒绝了佐岳外出
        public int larc_zuoyueOutgoingUsed;//佐岳外出走了几段了

        // 额外信息
        //public int fans; // 粉丝数，用于计算固有

        //当前回合的训练信息
        public int[,] personDistribution;//每个训练有哪些人头id，personDistribution[哪个训练][第几个人头]，空人头为-1

        public int larc_ssPersonsCount;//ss有几个人
        public int[] larc_ssPersons;//ss有哪几个人

        //这是个临时变量，导入ai的时候让它等于larc_ssPersonsCount即可
        //public int larc_ssPersonsCountLastTurn;//上个非比赛非远征回合有几个ss人头，只用来判断这个回合是不是新的ss，用来计算sss。为了避免满10人连出两个ss时计算错误，使用ss的时候把这个置零

        //通过计算获得的信息
        public int[,] trainValue;//第一个数是第几个训练，第二个数依次是速耐力根智pt体力
        public int[] failRate;//训练失败率

        //这些在模拟器里calculateTrain一下就行了
        //public int[] trainShiningNum;//这个训练有几个彩圈
        //int[] larc_staticBonus;//适性升级的收益，包括前5个1级和第6个的1级3级pt+10
        //public int[] larc_shixingPtGainAbroad;//海外训练适性pt收益
        //public int larc_trainBonus;//期待度训练加成

        //这两个可能有误差，但为了保证一致性，最好还是直接采用ai自己算的数值
        //public int[] larc_ssValue;//ss的速耐力根智（不包括上层的属性）
        //public int larc_ssFailRate;//ss的失败率

        public GameStatusSend_LArc(Gallop.SingleModeCheckEventResponse @event)
        {

            if ((@event.data.unchecked_event_array != null && @event.data.unchecked_event_array.Length > 0) || @event.data.race_start_info != null) return;

            umaId = @event.data.chara_info.card_id + 1000000 * @event.data.chara_info.rarity;
            int turnNum = @event.data.chara_info.turn;//游戏里回合数从1开始
            turn = turnNum - 1;//ai里回合数从0开始
            vital = @event.data.chara_info.vital;
            maxVital = @event.data.chara_info.max_vital;
            isQieZhe = @event.data.chara_info.chara_effect_id_array.Contains(7);
            isAiJiao = @event.data.chara_info.chara_effect_id_array.Contains(8);
            failureRateBias = 0;
            if (@event.data.chara_info.chara_effect_id_array.Contains(6))
            {
                failureRateBias = 2;
            }
            if (@event.data.chara_info.chara_effect_id_array.Contains(10))
            {
                failureRateBias = -2;
            }

            fiveStatus = new int[]
            {
                ScoreUtils.ReviseOver1200(@event.data.chara_info.speed),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.stamina),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.power) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.guts) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.wiz) ,
            };

            fiveStatusLimit = new int[]
            {
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_speed),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_stamina),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_power) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_guts) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_wiz) ,
            };

            skillPt = @event.data.chara_info.skill_point;

            try
            {
                double ptRate = isQieZhe ? 2.1 : 1.9;
                double ptScore = AiUtils.calculateSkillScore(@event, ptRate);
                skillPt=(int)(ptScore/ptRate);
            }
            catch(Exception ex)
            {
                AnsiConsole.MarkupLine("获取当前技能分失败"+ex.Message);
            }

            skillScore = 0;

            motivation = @event.data.chara_info.motivation;
            //fans = @event.data.chara_info.fans;
            cardId = new int[6];


            isPositiveThinking = @event.data.chara_info.chara_effect_id_array.Contains(25);

            bool LArcIsAbroad = (turnNum >= 37 && turnNum <= 43) || (turnNum >= 61 && turnNum <= 67);

            trainLevelCount = new int[5];
            for (int i = 0; i < 5; i++)
            {
                trainLevelCount[i] = (GameStats.stats[turnNum].trainLevel[i] - 1) * 4 + GameStats.stats[turnNum].trainLevelCount[i];
            }

            zhongMaBlueCount = new int[5];
            //用属性上限猜蓝因子个数
            {
                int[] defaultLimit = new int[5] { 2000, 2000, 1800, 1800, 1400 };
                double factor = 16;//每个三星因子可以提多少上限
                if (turn >= 54)//第二次继承结束
                    factor = 22;
                else if (turn >= 30)//第二次继承结束
                    factor = 19;
                for (int i = 0; i < 5; i++)
                {
                    int threeStarCount = (int)Math.Round((fiveStatusLimit[i] - defaultLimit[i]) / 2 / factor);
                    if (threeStarCount > 6) threeStarCount = 6;
                    if (threeStarCount < 0) threeStarCount = 0;
                    zhongMaBlueCount[i] = threeStarCount * 3;
                }
            }
            zhongMaExtraBonus = new int[6] { 10, 10, 30, 0, 10, 70 };//大师杯,青春杯,凯旋门因子混合

            motivationDropCount = GameStats.m_motDropCount;

            if (turnNum >= 3)
            {
                larc_supportPtAll = @event.data.arc_data_set.arc_rival_array.Sum(x => x.approval_point);
                larc_shixingPt = @event.data.arc_data_set.arc_info.global_exp;

                larc_isSSS = @event.data.arc_data_set.selection_info != null && @event.data.arc_data_set.selection_info.is_special_match == 1;//是否为sss
                larc_ssWin = @event.data.arc_data_set.arc_rival_array.Sum(x => x.star_lv);
                larc_ssWinSinceLastSSS = GameStats.m_contNonSSS;

                larc_levels = new int[10];
                for (int i = 0; i < 10; i++)
                    larc_levels[i] = @event.data.arc_data_set.arc_info.potential_array.First(x => x.potential_id == GameGlobal.LArcLessonMappingInv[i]).level;
            }
            else
            {
                larc_supportPtAll = 0;
                larc_shixingPt = 0;

                larc_isSSS = false;
                larc_ssWin = 0;
                larc_ssWinSinceLastSSS = 0;

                larc_levels = new int[10];
                for (int i = 0; i < 10; i++)
                    larc_levels[i] = 0;
            }

            //从游戏json的id到ai的人头编号的换算
            Dictionary<int, int> headIdConvert = new Dictionary<int, int>();
            foreach (var s in @event.data.chara_info.support_card_array)
            {
                int p = s.position - 1;
                //突破数+10*卡原来的id，例如神团是30137，满破神团就是301374
                cardId[p] = s.limit_break_count + s.support_card_id * 10;
            }

            int larc_zuoyueType = 0;
            persons = new LArcPerson[18];
            for (int i = 0; i < 18; i++)
                persons[i] = new LArcPerson();
            normalCardCount = 0;

            {
                //var friendCards = new List<int>  //各种友人团队卡
                //{
                //    30160,
                //    10094
                //};
                for (int i = 0; i < 6; i++)
                {
                    if (cardId[i] / 10 == 30160)//ssr佐岳
                    {
                        larc_zuoyueType = 1;
                        persons[17].cardIdInGame = i;
                        headIdConvert[i + 1] = 17;
                    }
                    else if (cardId[i] / 10 == 10094)//r佐岳
                    {
                        larc_zuoyueType = 2;
                        persons[17].cardIdInGame = i;
                        headIdConvert[i + 1] = 17;
                    }
                    else
                    {
                        persons[normalCardCount].cardIdInGame = i;
                        headIdConvert[i + 1] = normalCardCount;
                        normalCardCount += 1;
                    }
                }
            }

            if (larc_zuoyueType != 0)
            {

                var d = @event.data.chara_info.evaluation_info_array.First(x => x.target_id == headIdConvert.First(x => x.Value == 17).Key);
                larc_zuoyueOutgoingUnlocked=d.is_outing==1;//佐岳外出解锁
                larc_zuoyueOutgoingRefused=false;//无法从已知的信息中得出是否拒绝了外出。考虑到一般不会拒绝外出，所以默认没拒绝
                larc_zuoyueOutgoingUsed = d.story_step;//佐岳外出走了几段了


                larc_zuoyueFirstClick = false;//佐岳是否点过第一次
                for (int t = GameStats.currentTurn; t >= 1; t--)
                {
                    if (GameStats.stats[t] == null)
                    {
                        break;
                    }

                    if (!GameGlobal.TrainIds.Any(x => x == GameStats.stats[t].playerChoice)) //没训练
                        continue;
                    if (GameStats.stats[t].isTrainingFailed)//训练失败
                        continue;
                    if (!GameStats.stats[t].larc_zuoyueAtTrain[GameGlobal.ToTrainIndex[GameStats.stats[t].playerChoice]])
                        continue;//没点佐岳

                    larc_zuoyueFirstClick = true;
                    break;
                }

            }

            for (int i = 0; i < normalCardCount; i++)
                persons[i].personType = 2;
            for (int i = normalCardCount; i < 15; i++)
                persons[i].personType = 3;
            persons[15].personType = 4;
            persons[16].personType = 5;
            if (larc_zuoyueType == 0)
            {
                persons[17].personType = 6;
            }
            else
            {
                persons[17].personType = 1;
            }
            headIdConvert[102] = 15;
            headIdConvert[103] = 16;
            if(larc_zuoyueType==0)
            {
                headIdConvert[110] = 17;
            }

            if (turnNum >= 3)
            {
                int i = normalCardCount;
                foreach (var s in @event.data.arc_data_set.evaluation_info_array)
                {
                    //npc当且仅当s.chara_id==s.target_id
                    if(s.chara_id==s.target_id)
                    {
                        headIdConvert[s.chara_id] = i;
                        i += 1;
                    }
                }
                if (i != 15)
                {
                    throw new Exception("npc人数不正确，可能是因为带了佐岳以外的友人团队卡");
                }
            }

            //到目前为止，headIdConvert写完了

            //羁绊
            foreach (var s in @event.data.chara_info.evaluation_info_array)
            {
                if (!headIdConvert.ContainsKey(s.target_id))
                    continue;
                int p = headIdConvert[s.target_id];
                persons[p].friendship = s.evaluation;
            }

            //larc信息
            if (turnNum >= 3)
            {
                for (int i = 0; i < 15; i++)
                {
                    var chara_id = @event.data.arc_data_set.evaluation_info_array.First(x => x.target_id == headIdConvert.First(x => x.Value == i).Key).chara_id;
                    var p = @event.data.arc_data_set.arc_rival_array.First(x => x.chara_id == chara_id);

                    persons[i].larc_charge = p.rival_boost;
                    persons[i].larc_statusType = GameGlobal.ToTrainIndex[p.command_id];
                    persons[i].larc_specialBuff = GameStats.SSRivalsSpecialBuffs[chara_id];
                    if (persons[i].larc_specialBuff == 0) //不知道是什么buff，有可能是因为小黑板是半途开启的
                        persons[i].larc_specialBuff = 11;
                    persons[i].larc_level = p.star_lv + 1;
                    persons[i].larc_buffLevel = p.selection_peff_array.Min(x => x.effect_num);
                    persons[i].larc_nextThreeBuffs = new int[3];
                    for (int j = 0; j < 3; j++)
                    {
                        persons[i].larc_nextThreeBuffs[j] = p.selection_peff_array.First(x => x.effect_num == persons[i].larc_buffLevel + j).effect_group_id;
                    }
                }
            }

            personDistribution = new int[5, 5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    personDistribution[i, j] = -1;

            foreach (var train in @event.data.home_info.command_info_array)
            {
                if (!GameGlobal.ToTrainIndex.ContainsKey(train.command_id))//不是正常训练
                    continue;
                int trainId = GameGlobal.ToTrainIndex[train.command_id];

                int j = 0;
                foreach(var p in train.training_partner_array)
                {
                    int pid = headIdConvert[p];
                    personDistribution[trainId, j] = pid;
                    j += 1;
                }
                foreach (var p in train.tips_event_partner_array)
                {
                    int pid = headIdConvert[p];
                    persons[pid].isHint = true;
                }
            }

            //SS Match
            larc_ssPersonsCount = 0;
            larc_ssPersons = new int[5] { -1, -1, -1, -1, -1 };
            if (turnNum >= 3)
            {

                larc_ssPersonsCount = @event.data.arc_data_set.selection_info.selection_rival_info_array.Length;
                for (var i = 0; i < larc_ssPersonsCount; i++)
                {
                    var chara_id = @event.data.arc_data_set.selection_info.selection_rival_info_array[i].chara_id;
                    var pid = headIdConvert[@event.data.arc_data_set.evaluation_info_array.First(x => x.chara_id == chara_id).target_id];
                    larc_ssPersons[i] = pid;
                }
            }

            trainValue = new int[5, 7];
            failRate = new int[5];
            {
                var currentVital = @event.data.chara_info.vital;
                //maxVital = @event.data.chara_info.max_vital;
                var currentFiveValue = fiveStatus;

                var trainItems = new Dictionary<int, SingleModeCommandInfo>();
                if (@event.IsScenario(ScenarioType.LArc))
                {
                    //LArc的合宿ID不一样，所以要单独处理
                    trainItems.Add(101, @event.data.home_info.command_info_array.Any(x => x.command_id == 1101) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1101) : @event.data.home_info.command_info_array.First(x => x.command_id == 101));
                    trainItems.Add(105, @event.data.home_info.command_info_array.Any(x => x.command_id == 1102) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1102) : @event.data.home_info.command_info_array.First(x => x.command_id == 105));
                    trainItems.Add(102, @event.data.home_info.command_info_array.Any(x => x.command_id == 1103) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1103) : @event.data.home_info.command_info_array.First(x => x.command_id == 102));
                    trainItems.Add(103, @event.data.home_info.command_info_array.Any(x => x.command_id == 1104) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1104) : @event.data.home_info.command_info_array.First(x => x.command_id == 103));
                    trainItems.Add(106, @event.data.home_info.command_info_array.Any(x => x.command_id == 1105) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1105) : @event.data.home_info.command_info_array.First(x => x.command_id == 106));
                }
                else
                {
                    //速耐力根智，6xx为合宿时ID
                    trainItems.Add(101, @event.data.home_info.command_info_array.Any(x => x.command_id == 601) ? @event.data.home_info.command_info_array.First(x => x.command_id == 601) : @event.data.home_info.command_info_array.First(x => x.command_id == 101));
                    trainItems.Add(105, @event.data.home_info.command_info_array.Any(x => x.command_id == 602) ? @event.data.home_info.command_info_array.First(x => x.command_id == 602) : @event.data.home_info.command_info_array.First(x => x.command_id == 105));
                    trainItems.Add(102, @event.data.home_info.command_info_array.Any(x => x.command_id == 603) ? @event.data.home_info.command_info_array.First(x => x.command_id == 603) : @event.data.home_info.command_info_array.First(x => x.command_id == 102));
                    trainItems.Add(103, @event.data.home_info.command_info_array.Any(x => x.command_id == 604) ? @event.data.home_info.command_info_array.First(x => x.command_id == 604) : @event.data.home_info.command_info_array.First(x => x.command_id == 103));
                    trainItems.Add(106, @event.data.home_info.command_info_array.Any(x => x.command_id == 605) ? @event.data.home_info.command_info_array.First(x => x.command_id == 605) : @event.data.home_info.command_info_array.First(x => x.command_id == 106));
                }

                var trainStats = new TrainStats[5];
                var failureRate = new Dictionary<int, int>();
                for (int t = 0; t < 5; t++)
                {
                    int tid = GameGlobal.TrainIds[t];
                    failureRate[tid] = trainItems[tid].failure_rate;
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
                        if (GameGlobal.ToTrainId.ContainsKey(item.command_id) &&
                            GameGlobal.ToTrainId[item.command_id] == tid)
                        {
                            foreach (var trainParam in item.params_inc_dec_info_array)
                            {
                                trainParams[trainParam.target_type] += trainParam.value;
                            }
                        }
                    }

                    //剧本加成（上层显示）
                    foreach (var item in @event.data.arc_data_set.command_info_array)
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

                    var stats = new TrainStats();
                    stats.FailureRate = trainItems[tid].failure_rate;
                    stats.VitalGain = trainParams[10];
                    if (currentVital + stats.VitalGain > maxVital)
                        stats.VitalGain = maxVital - currentVital;
                    if (stats.VitalGain < -currentVital)
                        stats.VitalGain = -currentVital;
                    stats.FiveValueGain = new int[] { trainParams[1], trainParams[2], trainParams[3], trainParams[4], trainParams[5] };
                    for (int i = 0; i < 5; i++)
                        stats.FiveValueGain[i] = ScoreUtils.ReviseOver1200(currentFiveValue[i] + stats.FiveValueGain[i]) - ScoreUtils.ReviseOver1200(currentFiveValue[i]);
                    stats.PtGain = trainParams[30];
                    for (int i = 0; i < 5; i++)
                        trainValue[t, i] = stats.FiveValueGain[i];
                    trainValue[t, 5] = stats.PtGain;
                    trainValue[t, 6] = stats.VitalGain;
                    failRate[t] = stats.FailureRate;
                }
            }
        }    
    }
    public class GameStatusSend_Ura
    {
        public int umaId;//马娘编号，见KnownUmas.cpp
        //int16_t fiveStatusBonus[5];//马娘的五维属性的成长率
        public int turn;//回合数，从0开始，到77结束
        public int vital;//体力，叫做“vital”是因为游戏里就这样叫的
        public int maxVital;//体力上限
        public bool isQieZhe;//切者
        public bool isAiJiao;//爱娇
        public int failureRateBias;//失败率改变量。练习上手=2，练习下手=-2
        public int[] fiveStatus;//五维属性，1200以上不减半
        public int[] fiveStatusLimit;//五维属性上限，1200以上不减半
        public int skillPt;//技能点
        public int skillScore;//已买技能的分数
        public int motivation;//干劲，从1到5分别是绝不调到绝好调
        public bool isPositiveThinking;//ポジティブ思考，友人第三段出行选上的buff，可以防一次掉心情
        public int[] trainLevelCount;//五个训练的等级的计数，实际训练等级=min(5,t/12+1)
        //public int[] zhongMaBlueCount;//种马的蓝因子个数，假设只有3星
        //public int[] zhongMaExtraBonus;//种马的剧本因子以及技能白因子（等效成pt），每次继承加多少。全大师杯因子典型值大约是30速30力200pt
        public int normalCardCount;//速耐力根智卡的数量
        public int[] cardId;//6张卡的id
        //SupportCard cardParam[6];//六张卡的参数，拷贝到Game类里，一整局内不变，顺序任意。这样做的目的是训练ai时可能要随机改变卡的参数提高鲁棒性，所以每个game的卡的参数可能不一样
        //int16_t saihou;//赛后加成
        public UraPerson[] persons;//如果不带其他友人团队卡，最多10个头。依次是普通支援卡（顺序随意）：0~3到5，绿帽支援卡6, 理事长7，记者8，桐生院9（带没带卡都是9），
        //bool isRacing;//这个回合是否在比赛

        public int motivationDropCount;//掉过几次心情了，不包括剧本事件（已知同一个掉心情不会出现多次，一共3个掉心情事件，所以之前掉过越多，之后掉的概率越低）

        //public int ura_tsyType;//没带桐生院=0，带的SR卡=1，带的R卡=2
        //public int ura_lmType;//没带绿帽=0，带的SSR卡=1，带的R卡=2
        //public double larc_zuoyueVitalBonus;//佐岳卡的回复量倍数（满破1.8）
        //public double larc_zuoyueStatusBonus;//佐岳卡的事件效果倍数（满破1.2）
        public bool ura_tsyFirstClick;//桐生院是否点过第一次
        public bool ura_tsyOutgoingUnlocked;//桐生院外出解锁
        public bool ura_tsyOutgoingRefused;//是否拒绝了桐生院外出
        public int ura_tsyOutgoingUsed;//桐生院外出走了几段了

        public bool ura_lmFirstClick;//绿帽是否点过第一次
        public bool ura_lmOutgoingUnlocked;//绿帽外出解锁
        public bool ura_lmOutgoingRefused;//是否拒绝了绿帽外出
        public int ura_lmOutgoingUsed;//绿帽外出走了几段了

        // 额外信息
        //public int fans; // 粉丝数，用于计算固有

        //当前回合的训练信息
        public int[,] personDistribution;//每个训练有哪些人头id，personDistribution[哪个训练][第几个人头]，空人头为-1

        //这是个临时变量，导入ai的时候让它等于larc_ssPersonsCount即可
        //public int larc_ssPersonsCountLastTurn;//上个非比赛非远征回合有几个ss人头，只用来判断这个回合是不是新的ss，用来计算sss。为了避免满10人连出两个ss时计算错误，使用ss的时候把这个置零

        //通过计算获得的信息
        public int[,] trainValue;//第一个数是第几个训练，第二个数依次是速耐力根智pt体力
        public int[] failRate;//训练失败率

        //这些在模拟器里calculateTrain一下就行了
        //public int[] trainShiningNum;//这个训练有几个彩圈
        //int[] larc_staticBonus;//适性升级的收益，包括前5个1级和第6个的1级3级pt+10
        //public int[] larc_shixingPtGainAbroad;//海外训练适性pt收益
        //public int larc_trainBonus;//期待度训练加成

        //这两个可能有误差，但为了保证一致性，最好还是直接采用ai自己算的数值
        //public int[] larc_ssValue;//ss的速耐力根智（不包括上层的属性）
        //public int larc_ssFailRate;//ss的失败率

        //以下为自定义
        public Gallop.SkillData[] skills;//已学习
        public SkillTips[] skillTips;//已获得启示
        public int[] disable_skill_id_array;//已消除负面技能
        public int[] chara_effect_id_array;//状态
        public int[] available_command_array;//所有有效的出行
        public int[][] proper_info;//适性信息，用于技能选择
        public int talent_level;//觉醒等级，用于计算觉醒技能

        public GameStatusSend_Ura(Gallop.SingleModeCheckEventResponse @event)
        {

            //if ((@event.data.unchecked_event_array != null && @event.data.unchecked_event_array.Length > 0) || @event.data.race_start_info != null) return;
            
            skills = @event.data.chara_info.skill_array;
            skillTips = @event.data.chara_info.skill_tips_array;
            chara_effect_id_array = @event.data.chara_info.chara_effect_id_array;
            disable_skill_id_array = @event.data.chara_info.disable_skill_id_array;

            umaId = @event.data.chara_info.card_id + 1000000 * @event.data.chara_info.rarity;
            talent_level = @event.data.chara_info.talent_level;
            int turnNum = @event.data.chara_info.turn;//游戏里回合数从1开始
            turn = turnNum - 1;//ai里回合数从0开始
            vital = @event.data.chara_info.vital;
            maxVital = @event.data.chara_info.max_vital;
            isQieZhe = @event.data.chara_info.chara_effect_id_array.Contains(7);
            isAiJiao = @event.data.chara_info.chara_effect_id_array.Contains(8);
            failureRateBias = 0;
            if (@event.data.chara_info.chara_effect_id_array.Contains(6))
            {
                failureRateBias = 2;
            }
            if (@event.data.chara_info.chara_effect_id_array.Contains(10))
            {
                failureRateBias = -2;
            }


            fiveStatus = new int[]
            {
                ScoreUtils.ReviseOver1200(@event.data.chara_info.speed),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.stamina),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.power) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.guts) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.wiz) ,
            };

            fiveStatusLimit = new int[]
            {
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_speed),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_stamina),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_power) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_guts) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_wiz) ,
            };


            skillPt = @event.data.chara_info.skill_point;

            try
            {
                double ptRate = isQieZhe ? 2.1 : 1.9;
                double ptScore = AiUtils.calculateSkillScore(@event, ptRate);
                skillScore = (int)(ptScore / ptRate);
                AnsiConsole.MarkupLine($"当前技能分: {skillScore}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("获取当前技能分失败" + ex.Message);
#if DEBUG
                throw;
#endif
            }

            //skillScore = 0;

            motivation = @event.data.chara_info.motivation;
            //fans = @event.data.chara_info.fans;
            cardId = new int[6];

            isPositiveThinking = @event.data.chara_info.chara_effect_id_array.Contains(25);

            available_command_array = [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1];
            //bool LArcIsAbroad = (turnNum >= 37 && turnNum <= 43) || (turnNum >= 61 && turnNum <= 67);

            trainLevelCount = new int[5];
            for (int i = 0; i < 5; i++)
            {
                trainLevelCount[i] = (GameStats.stats[turnNum]?.trainLevel[i]??1 - 1) * 4 + GameStats.stats[turnNum]?.trainLevelCount[i]??0;
            }

            motivationDropCount = GameStats.m_motDropCount;
            persons = new UraPerson[10];
            personDistribution = new int[5, 5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    personDistribution[i, j] = -1;
            trainValue = new int[5, 7];
            failRate = new int[5];
            proper_info =
                [
                    [@event.data.chara_info.proper_ground_turf, @event.data.chara_info.proper_ground_dirt],
                    [
                        @event.data.chara_info.proper_distance_short,
                        @event.data.chara_info.proper_distance_mile,
                        @event.data.chara_info.proper_distance_middle,
                        @event.data.chara_info.proper_distance_long
                    ],
                    [
                        @event.data.chara_info.proper_running_style_nige,
                        @event.data.chara_info.proper_running_style_senko,
                        @event.data.chara_info.proper_running_style_sashi,
                        @event.data.chara_info.proper_running_style_oikomi
                    ]
                ];

            //从游戏json的id到ai的人头编号的换算
            Dictionary<int, int> headIdConvert = new Dictionary<int, int>();
            foreach (var s in @event.data.chara_info.support_card_array)
            {
                int p = s.position - 1;
                //突破数+10*卡原来的id，例如神团是30137，满破神团就是301374
                cardId[p] = s.limit_break_count + s.support_card_id * 10;
            }

            int ura_tsyType = 0;
            int ura_lmType = 0;
            for (int i = 0; i < 10; i++)
                persons[i] = new UraPerson();
            normalCardCount = 0;

            {
                //var friendCards = new List<int>  //各种友人团队卡
                //{
                //    10021,  //r駿川たづな
                //    10022,  //r桐生院葵
                //    10060,  //r樫本理子
                //    10094,  //r佐岳メイ
                //    20021,  //sr桐生院葵
                //    30021,  //ssr駿川たづな
                //    30036,  //ssr樫本理子
                //    30160,  //ssr佐岳メイ
                //    10074,  //r安心沢刺々美
                //    30067,  //ssr玉座に集いし者たち
                //    30080,  //ssr安心沢刺々美
                //    30137,  //ssr祖にして導く者
                //};
                for (int i = 0; i < 6; i++)
                {
                    if (cardId[i] / 10 == 20021)//sr桐生院
                    {
                        ura_tsyType = 1;
                        persons[9].cardIdInGame = i;
                        headIdConvert[i + 1] = 9;
                        persons[9].trainType = 0;
                    }
                    else if (cardId[i] / 10 == 10022)//r桐生院
                    {
                        ura_tsyType = 2;
                        persons[9].cardIdInGame = i;
                        headIdConvert[i + 1] = 9;
                        persons[9].trainType = 0;
                    }
                    else if (cardId[i] / 10 == 30021)//ssr绿帽
                    {
                        ura_lmType = 1;
                        persons[6].cardIdInGame = i;
                        headIdConvert[i + 1] = 6;
                        persons[6].trainType = 0;
                    }
                    else if (cardId[i] / 10 == 10021)//r绿帽
                    {
                        ura_lmType = 2;
                        persons[6].cardIdInGame = i;
                        headIdConvert[i + 1] = 6;
                        persons[6].trainType = 0;
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

            if (ura_tsyType != 0)
            {
                var d = @event.data.chara_info.evaluation_info_array.First(x => x.target_id == headIdConvert.First(x => x.Value == 9).Key);
                ura_tsyOutgoingUnlocked = d.is_outing == 1;//桐生院外出解锁
                ura_tsyOutgoingRefused = false;//无法从已知的信息中得出是否拒绝了外出。考虑到一般不会拒绝外出，所以默认没拒绝
                ura_tsyOutgoingUsed = d.story_step;//桐生院外出走了几段了
                ura_tsyFirstClick = false;//桐生院是否点过第一次
                for (int t = GameStats.currentTurn; t >= 1; t--)
                {
                    if (GameStats.stats[t] == null)
                    {
                        break;
                    }

                    if (!GameGlobal.TrainIds.Any(x => x == GameStats.stats[t].playerChoice)) //没训练
                        continue;
                    if (GameStats.stats[t].isTrainingFailed)//训练失败
                        continue;
                    if (!GameStats.stats[t].ura_tsyAtTrain[GameGlobal.ToTrainIndex[GameStats.stats[t].playerChoice]])
                        continue;//没点桐生院

                    ura_tsyFirstClick = true;
                    break;
                }
            }
            if (ura_lmType != 0)
            {
                var d = @event.data.chara_info.evaluation_info_array.First(x => x.target_id == headIdConvert.First(x => x.Value == 6).Key);
                ura_lmOutgoingUnlocked = d.is_outing == 1;//绿帽外出解锁
                ura_lmOutgoingRefused = false;//无法从已知的信息中得出是否拒绝了外出。考虑到一般不会拒绝外出，所以默认没拒绝
                ura_lmOutgoingUsed = d.story_step;//绿帽外出走了几段了

                ura_lmFirstClick = false;//绿帽是否点过第一次
                for (int t = GameStats.currentTurn; t >= 1; t--)
                {
                    if (GameStats.stats[t] == null)
                    {
                        break;
                    }

                    if (!GameGlobal.TrainIds.Any(x => x == GameStats.stats[t].playerChoice)) //没训练
                        continue;
                    if (GameStats.stats[t].isTrainingFailed)//训练失败
                        continue;
                    if (!GameStats.stats[t].ura_lmAtTrain[GameGlobal.ToTrainIndex[GameStats.stats[t].playerChoice]])
                        continue;//没点绿帽

                    ura_lmFirstClick = true;
                    break;
                }
            }

            for (int i = 0; i < normalCardCount; i++)
                persons[i].personType = 2;
            //for (int i = normalCardCount; i < 6; i++)
            //    persons[i].personType = 3;
            persons[6].personType = ura_lmType == 0 ? 0 : 3;
            persons[7].personType = 4;
            persons[8].personType = 5;
            persons[9].personType = ura_tsyType == 0 ? 6 : 1;

            if (ura_lmType == 0)
                headIdConvert[101] = 6;
            headIdConvert[102] = 7;
            headIdConvert[103] = 8;
            if (ura_tsyType == 0)
                headIdConvert[104] = 9;

            //if (turnNum >= 3)
            //{
                //int i = normalCardCount;
                //foreach (var s in @event.data.evaluation_info_array)
                //{
                    //npc当且仅当s.chara_id==s.target_id
                //    if (s.chara_id == s.target_id)
                //    {
                //        headIdConvert[s.chara_id] = i;
                //        i += 1;
                //    }
                //}
                //if (i != 15)
                //{
                //    throw new Exception("npc人数不正确，可能是因为带了佐岳以外的友人团队卡");
                //}
            //}

            //到目前为止，headIdConvert写完了

            //羁绊
            foreach (var s in @event.data.chara_info.evaluation_info_array)
            {
                if (!headIdConvert.ContainsKey(s.target_id))
                    continue;
                int p = headIdConvert[s.target_id];
                persons[p].friendship = s.evaluation;
            }

            if (@event.data.home_info == null) return;
            int available_command_num = 0;
            foreach (var train in @event.data.home_info.command_info_array)
            {
                if (train.is_enable > 0)
                    available_command_array[available_command_num++] = train.command_id;
                if (!GameGlobal.ToTrainIndex.ContainsKey(train.command_id))//不是正常训练
                    continue;
                int trainId = GameGlobal.ToTrainIndex[train.command_id];

                int j = 0;
                foreach (var p in train.training_partner_array)
                {
                    int pid = headIdConvert[p];
                    personDistribution[trainId, j] = pid;
                    j += 1;
                }
                foreach (var p in train.tips_event_partner_array)
                {
                    int pid = headIdConvert[p];
                    persons[pid].isHint = true;
                }
            }


            {
                var currentVital = @event.data.chara_info.vital;
                //maxVital = @event.data.chara_info.max_vital;
                var currentFiveValue = fiveStatus;

                var trainItems = new Dictionary<int, SingleModeCommandInfo>();
                if (@event.IsScenario(ScenarioType.LArc))
                {
                    //LArc的合宿ID不一样，所以要单独处理
                    trainItems.Add(101, @event.data.home_info.command_info_array.Any(x => x.command_id == 1101) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1101) : @event.data.home_info.command_info_array.First(x => x.command_id == 101));
                    trainItems.Add(105, @event.data.home_info.command_info_array.Any(x => x.command_id == 1102) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1102) : @event.data.home_info.command_info_array.First(x => x.command_id == 105));
                    trainItems.Add(102, @event.data.home_info.command_info_array.Any(x => x.command_id == 1103) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1103) : @event.data.home_info.command_info_array.First(x => x.command_id == 102));
                    trainItems.Add(103, @event.data.home_info.command_info_array.Any(x => x.command_id == 1104) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1104) : @event.data.home_info.command_info_array.First(x => x.command_id == 103));
                    trainItems.Add(106, @event.data.home_info.command_info_array.Any(x => x.command_id == 1105) ? @event.data.home_info.command_info_array.First(x => x.command_id == 1105) : @event.data.home_info.command_info_array.First(x => x.command_id == 106));
                }
                else
                {
                    //速耐力根智，6xx为合宿时ID
                    trainItems.Add(101, @event.data.home_info.command_info_array.Any(x => x.command_id == 601) ? @event.data.home_info.command_info_array.First(x => x.command_id == 601) : @event.data.home_info.command_info_array.First(x => x.command_id == 101));
                    trainItems.Add(105, @event.data.home_info.command_info_array.Any(x => x.command_id == 602) ? @event.data.home_info.command_info_array.First(x => x.command_id == 602) : @event.data.home_info.command_info_array.First(x => x.command_id == 105));
                    trainItems.Add(102, @event.data.home_info.command_info_array.Any(x => x.command_id == 603) ? @event.data.home_info.command_info_array.First(x => x.command_id == 603) : @event.data.home_info.command_info_array.First(x => x.command_id == 102));
                    trainItems.Add(103, @event.data.home_info.command_info_array.Any(x => x.command_id == 604) ? @event.data.home_info.command_info_array.First(x => x.command_id == 604) : @event.data.home_info.command_info_array.First(x => x.command_id == 103));
                    trainItems.Add(106, @event.data.home_info.command_info_array.Any(x => x.command_id == 605) ? @event.data.home_info.command_info_array.First(x => x.command_id == 605) : @event.data.home_info.command_info_array.First(x => x.command_id == 106));
                }

                var trainStats = new TrainStats[5];
                var failureRate = new Dictionary<int, int>();
                for (int t = 0; t < 5; t++)
                {
                    int tid = GameGlobal.TrainIds[t];
                    failureRate[tid] = trainItems[tid].failure_rate;
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
                        if (GameGlobal.ToTrainId.ContainsKey(item.command_id) &&
                            GameGlobal.ToTrainId[item.command_id] == tid)
                        {
                            foreach (var trainParam in item.params_inc_dec_info_array)
                            {
                                trainParams[trainParam.target_type] += trainParam.value;
                            }
                        }
                    }

                    //剧本加成（上层显示）
                    /*foreach (var item in @event.data.arc_data_set.command_info_array)
                    {
                        if (GameGlobal.ToTrainId.ContainsKey(item.command_id) &&
                            GameGlobal.ToTrainId[item.command_id] == tid)
                        {
                            foreach (var trainParam in item.params_inc_dec_info_array)
                            {
                                trainParams[trainParam.target_type] += trainParam.value;
                            }
                        }
                    }*/

                    var stats = new TrainStats();
                    stats.FailureRate = trainItems[tid].failure_rate;
                    stats.VitalGain = trainParams[10];
                    if (currentVital + stats.VitalGain > maxVital)
                        stats.VitalGain = maxVital - currentVital;
                    if (stats.VitalGain < -currentVital)
                        stats.VitalGain = -currentVital;
                    stats.FiveValueGain = new int[] { trainParams[1], trainParams[2], trainParams[3], trainParams[4], trainParams[5] };
                    for (int i = 0; i < 5; i++)
                        stats.FiveValueGain[i] = ScoreUtils.ReviseOver1200(currentFiveValue[i] + stats.FiveValueGain[i]) - ScoreUtils.ReviseOver1200(currentFiveValue[i]);
                    stats.PtGain = trainParams[30];
                    for (int i = 0; i < 5; i++)
                        trainValue[t, i] = stats.FiveValueGain[i];
                    trainValue[t, 5] = stats.PtGain;
                    trainValue[t, 6] = stats.VitalGain;
                    failRate[t] = stats.FailureRate;
                }//for
            }
        }
    }
    public class EventSend {
        public string triggerName;//触发者名称
        public string eventName;//事件名称
        public int story_id;//事件编号
        public string[] choices;//选项
        public int[] select_indices;//隐藏
        public State[] is_success;//是否成功 -1未知 0失败 1成功 2大成功 max中性
        public string[] effect;//效果

        public SingleModeEventInfo eventInfo; //原始数据
        public EventSend(SingleModeCheckEventResponse @event, int index) {
            eventInfo = @event.data.unchecked_event_array[index - 1];
            var length = eventInfo.event_contents_info.choice_array.Length;
            choices = new string[length];
            select_indices = new int[length];
            is_success = new State[length];
            effect = new string[length];
            //收录在数据库中
            if (Database.Events.TryGetValue(eventInfo.story_id, out var story))
            {
                triggerName = story.TriggerName;
                eventName = story.Name;
                story_id = eventInfo.story_id;
                for (var j = 0; j < length; ++j)
                {
                    var originalChoice = new Choice();
                    if (story.Choices.Count < (j + 1))
                    {
                        originalChoice.Option = "未知选项";
                        originalChoice.SuccessEffect = "未知效果";
                        originalChoice.FailedEffect = "未知效果";
                    }
                    else
                    {
                        originalChoice = story.Choices[j][0]; //因为kamigame的事件无法直接根据SelectIndex区分成功与否，所以必然只会有一个Choice;
                    }
                    //显示选项
                    choices[j] = string.IsNullOrEmpty(originalChoice.Option) ? "无选项" : originalChoice.Option;
                    select_indices[j] = eventInfo.event_contents_info.choice_array[j].select_index;
                    if (Database.SuccessEvent.TryGetValue(eventInfo.story_id, out var successEvent) && successEvent.Choices.Length > j) //是可以成功的事件且已在数据库中
                        AddLoggedEvent(successEvent.Choices[j]);
                    else
                        AddNormalEvent();
                    void AddLoggedEvent(SuccessChoice[] choices)
                    {
                        var find = choices.WithSelectIndex(eventInfo.event_contents_info.choice_array[j].select_index)
                            .WithScenarioId(@event.data.chara_info.scenario_id)
                            .TryGet(out var choice);
                        if (find)
                        {
                            effect[j] = choice.Effect;
                            is_success[j] = choice.State;
                        }                            
                        else
                        {
                            if (string.IsNullOrEmpty(originalChoice.FailedEffect))
                            {
                                effect[j] = originalChoice.SuccessEffect;
                                is_success[j] = State.None;
                            }
                            else
                            {
                                effect[j] = originalChoice.FailedEffect;
                                is_success[j] = State.Fail;
                            }                            
                        }                            
                    }
                    void AddNormalEvent()
                    {
                        //如果没有失败效果则显示成功效果（别问我为什么这么设置，问kamigame
                        if (string.IsNullOrEmpty(originalChoice.FailedEffect))
                        {
                            effect[j] = originalChoice.SuccessEffect;
                            is_success[j] = State.None;
                        }                            
                        else if (originalChoice.SuccessEffect == "未知效果" && originalChoice.FailedEffect == "未知效果")
                        {
                            effect[j] = "未知效果";
                            is_success[j] = State.None;
                        }
                        else
                        {
                            effect[j] = $"(成功时){originalChoice.SuccessEffect}{Environment.NewLine}(失败时){originalChoice.FailedEffect}";
                            is_success[j] = State.Fail;
                        }
                    }
                }
            }
            else //未知事件，直接显示ChoiceIndex
            {
                triggerName = "未知来源";
                eventName = "未知事件";
                story_id = eventInfo != null ? eventInfo.story_id: 0;
                for (var j = 0; j < length; ++j)
                {
                    choices[j] = "未知选项";
                    select_indices[j] = eventInfo.event_contents_info.choice_array[j].select_index;
                    is_success[j] = State.Unknown;
                    effect[j] = "未知效果";
                }
            }
        }
    }
    public class TeamStadiumSend
    {
        public int[,,] proper;
        public int[,] style;
        public int[,][] attribute;
        public TeamStadiumSend(TeamStadiumOpponentListResponse @event)
        {
            proper = new int[3,15,3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 15; j++)
                    for (int k = 0; k < 3; k++)
                        proper[i, j, k] = -1;
            style = new int[3,15];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 15; j++)
                    style[i, j] = -1;
            attribute = new int[3,15][];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 15; j++)
                    attribute[i, j] = [];
            foreach (var opponent in @event.data.opponent_info_array)
            {
                foreach (var chara in opponent.team_data_array.Where(x=>x.trained_chara_id != 0))
                {
                    int index = chara.distance_type * 3 + chara.member_id - 4;
                    style[opponent.strength-1, index] = chara.running_style;
                    var trainedChara = opponent.trained_chara_array.First(x => x.trained_chara_id == chara.trained_chara_id);
                    attribute[opponent.strength - 1, index] = [
                        trainedChara.speed,
                        trainedChara.stamina,
                        trainedChara.power,
                        trainedChara.guts,
                        trainedChara.wiz
                    ];
                    proper[opponent.strength - 1, index, 0] = (chara.distance_type switch
                    {
                        5 => trainedChara.proper_ground_dirt,
                        _ => trainedChara.proper_ground_turf
                    });
                    proper[opponent.strength - 1, index, 1] = (chara.distance_type switch
                    {
                        1 => trainedChara.proper_distance_short,
                        2 => trainedChara.proper_distance_mile,
                        3 => trainedChara.proper_distance_middle,
                        4 => trainedChara.proper_distance_long,
                        5 => trainedChara.proper_distance_mile
                    });
                    proper[opponent.strength - 1, index, 2] = (chara.running_style switch
                    {
                        1 => trainedChara.proper_running_style_nige,
                        2 => trainedChara.proper_running_style_senko,
                        3 => trainedChara.proper_running_style_sashi,
                        4 => trainedChara.proper_running_style_oikomi
                    });
                }
            }
        }
    }
}
