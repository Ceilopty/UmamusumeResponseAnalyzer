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
    public class UATPerson
    {
        public int personType;//0代表未加载（例如前两个回合的npc），1代表桐生院支援卡（R或SR都行），2代表普通支援卡，3代表绿帽支援卡，4理事长，5记者，6不带卡的桐生院。暂不支持其他友人/团队卡
        //int16_t cardId;//支援卡id，不是支援卡就0
        public int charaId;//npc人头的马娘id，不是npc就0，懒得写也可以一律0（只用于获得npc的名字）

        public int cardIdInGame;// Game.cardParam里的支援卡序号，非支援卡为-1
        public int friendship;//羁绊
                            //bool atTrain[5];//是否在五个训练里。对于普通的卡只是one-hot或者全空，对于ssr佐岳可能有两个true
                            //bool isShining;//是否闪彩。无法闪彩的卡或者npc恒为false
        public bool isHint;//是否有hint。友人卡或者npc恒为false
        public int cardRecord;//记录一些可能随着时间而改变的参数，例如根涡轮的固有

        //bool larc_isLinkCard;//是否为link支援卡
        //isShining, larc_isLinkCard, distribution 在ai里计算
        public int trainType;//如果是支援卡，则是其names.br中的Type；否则为-1
        public int member_state;

        public UATPerson()
        {
            personType = 0;
            charaId = 0;
            cardIdInGame = -1;
            friendship = 0;
            isHint = false;
            cardRecord = 0;
            trainType = -1;
            member_state = 0;
        }
    }
    public class GameStatusSend_UAT
    {
        public int scenario;//剧本
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
        public UATPerson[] persons;//如果不带其他友人团队卡，最多10个头。依次是普通支援卡（顺序随意）：0~3到5，绿帽支援卡6, 理事长7，记者8，桐生院9（带没带卡都是9），
        //bool isRacing;//这个回合是否在比赛

        public int motivationDropCount;//掉过几次心情了，不包括剧本事件（已知同一个掉心情不会出现多次，一共3个掉心情事件，所以之前掉过越多，之后掉的概率越低）

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

        public int lockedTrainingId;
        public int friendship_noncard_yayoi;//非卡理事长的羁绊，带了理事长卡就是0
        public int friendship_noncard_reporter;//非卡记者的羁绊       

        //单独处理友人卡，因为接近必带。其他友人团队卡的以后再考虑
        public int friend_type;//0没带友人卡，1 ssr卡，2 r卡
        public int friend_cardId;   // 单独存放友人卡ID
        public int friend_personId;//友人卡在persons里的编号
        public int friend_stage;//0未点击，1点击还未解锁出行，2已解锁出行
        public int friend_outgoingUsed;//出行已经走了几段了   暂时不考虑其他友人团队卡的出行

        //以下为自定义
        public Gallop.SkillData[] skills;//已学习
        public Gallop.SkillTips[] skillTips;//已获得启示
        public int[] disable_skill_id_array;//已消除负面技能
        public int[] chara_effect_id_array;//状态
        public int[] available_command_array;//所有有效的出行
        public int[][] proper_info;//适性信息，用于技能选择
        public int talent_level;//觉醒等级，用于计算觉醒技能

        public GameStatusSend_UAT(Gallop.SingleModeCheckEventResponse @event)
        {
            //事件也要更新AI Info，不能收工
            //if ((@event.data.unchecked_event_array != null && @event.data.unchecked_event_array.Length > 0) || @event.data.race_start_info != null) return;
            scenario = @event.data.chara_info.scenario_id;

            skills = @event.data.chara_info.skill_array;
            skillTips = @event.data.chara_info.skill_tips_array;
            chara_effect_id_array = @event.data.chara_info.chara_effect_id_array;
            disable_skill_id_array = @event.data.chara_info.disable_skill_id_array;

            umaId = @event.data.chara_info.card_id + 1000000 * @event.data.chara_info.rarity;
            talent_level = @event.data.chara_info.talent_level;
            var turnNum = @event.data.chara_info.turn;//游戏里回合数从1开始
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

            fiveStatus =
            [
                ScoreUtils.ReviseOver1200(@event.data.chara_info.speed),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.stamina),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.power) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.guts) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.wiz) ,
            ];

            fiveStatusLimit =
            [
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_speed),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_stamina),
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_power) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_guts) ,
                ScoreUtils.ReviseOver1200(@event.data.chara_info.max_wiz) ,
            ];

            skillPt = @event.data.chara_info.skill_point;

            try
            {
                var ptRate = isQieZhe ? 2.1 : 1.9;
                var ptScore = AiUtils.calculateSkillScore(@event, ptRate);
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
            foreach (var s in @event.data.chara_info.support_card_array)
            {
                var p = s.position - 1;
                //突破数+10*卡原来的id，例如神团是30137，满破神团就是301374
                cardId[p] = s.limit_break_count + s.support_card_id * 10;
            }

            isPositiveThinking = @event.data.chara_info.chara_effect_id_array.Contains(25);

            available_command_array = [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1];
            //bool LArcIsAbroad = (turnNum >= 37 && turnNum <= 43) || (turnNum >= 61 && turnNum <= 67);

            trainLevelCount = new int[5];
            for (var i = 0; i < 5; i++)
            {
                trainLevelCount[i] = (GameStats.stats[turnNum]?.trainLevel[i]??1 - 1) * 4 + GameStats.stats[turnNum]?.trainLevelCount[i]??0;
            }

            motivationDropCount = GameStats.m_motDropCount;
            persons = new UATPerson[10];
            personDistribution = new int[5, 5];
            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 5; j++)
                    personDistribution[i, j] = -1;
            }

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
            

            friendship_noncard_yayoi = @event.data.chara_info.evaluation_info_array.Any(x => x.target_id == 102) ?
                @event.data.chara_info.evaluation_info_array.First(x => x.target_id == 102).evaluation : 0;
            friendship_noncard_reporter = @event.data.chara_info.evaluation_info_array.Any(x => x.target_id == 103) ?
                @event.data.chara_info.evaluation_info_array.First(x => x.target_id == 103).evaluation : 0;

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
                        if (!GameStats.stats[t].cook_friendAtTrain[GameGlobal.ToTrainIndex[GameStats.stats[t].playerChoice]])
                            continue;//没点友人

                        friendClicked = true;
                        break;
                    }
                    if (friendClicked) friend_stage = 1;
                    else friend_stage = 0;
                }

            }
            else
            {
                friend_outgoingUsed = 0;
            }

            if (@event.data.home_info != null)
            {
                //计算Lockedtrainid
                {
                    var istrainlocked = false;
                    var enableidx = -1;
                    var command = @event.data.home_info.command_info_array;
                    foreach (var train in @event.data.home_info.command_info_array)
                    {
                        if (!GameGlobal.ToTrainIndex.ContainsKey(train.command_id))//不是正常训练
                            continue;
                        if (train.is_enable != 1)
                        {
                            istrainlocked = true;
                        }
                        else
                        {
                            enableidx = Convert.ToInt32(train.command_id) % 10;
                        }
                    }

                    if (istrainlocked)
                    {
                        lockedTrainingId = enableidx;
                    }
                    else
                    {
                        lockedTrainingId = -1;
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
                var failureRate = new Dictionary<int, int>();
                for (var t = 0; t < 5; t++)
                {
                    var tid = GameGlobal.TrainIds[t];
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
                        if (GameGlobal.ToTrainId.TryGetValue(item.command_id, out var value) && value == tid)
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

                    var stats = new TrainStats
                    {
                        FailureRate = trainItems[tid].failure_rate,
                        VitalGain = trainParams[10]
                    };
                    if (currentVital + stats.VitalGain > maxVital)
                        stats.VitalGain = maxVital - currentVital;
                    if (stats.VitalGain < -currentVital)
                        stats.VitalGain = -currentVital;
                    stats.FiveValueGain = new int[] { trainParams[1], trainParams[2], trainParams[3], trainParams[4], trainParams[5] };
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

        public void doSend()
        {
#if DEBUG
            AnsiConsole.MarkupLine("写入AI文件");
#endif
            var currentGSdirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "GameData", "Turn");
            Directory.CreateDirectory(currentGSdirectory);
            var success = false;
            var tried = 0;
            do
            {
                try
                {
                    var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }; // 去掉空值避免C++端抽风
                    File.WriteAllText($@"{currentGSdirectory}/thisTurn.json", JsonConvert.SerializeObject(this, Formatting.Indented, settings));
                    File.WriteAllText($@"{currentGSdirectory}/turn{this.turn}.json", JsonConvert.SerializeObject(this, Formatting.Indented, settings));
                    success = true; // 写入成功，跳出循环
                    break;
                }
                catch
                {
                    tried++;
                    AnsiConsole.MarkupLine("[yellow]写入失败[/]");
                }
            } while (!success && tried < 10);
            if (!success)
            {
                AnsiConsole.MarkupLine($@"[red]写入{currentGSdirectory}/thisTurn.json失败！[/]");
            }
        }
    }
}
