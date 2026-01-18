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
    public class UraPerson:UATPerson
    {
        //0代表未加载（例如前两个回合的npc），1代表桐生院支援卡（R或SR都行），2代表普通支援卡，3代表绿帽支援卡，4理事长，5记者，6不带卡的桐生院，7米可。暂不支持其他友人/团队卡
        //public new int personType = 0;
    }
    public class GameStatusSend_Ura:GameStatusSend_UAT
    {
        public new UraPerson[] persons;//如果不带其他友人团队卡，最多11个头。依次是普通支援卡（顺序随意）：0~3到5，绿帽支援卡6, 理事长7，记者8，桐生院9（带没带卡都是9），米克10
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

        public int versus_level = 0;//米可对决等级
        public bool versus_event = false;//米可对决发生
        public GameStatusSend_Ura (Gallop.SingleModeCheckEventResponse @event) : base(@event)
        {
            persons = new UraPerson[11];
            personDistribution = new int[5, 5];
            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 5; j++)
                    personDistribution[i, j] = -1;
            }


            //从游戏json的id到ai的人头编号的换算
            var headIdConvert = new Dictionary<int, int>();
            var ura_tsyType = 0;
            var ura_lmType = 0;
            for (var i = 0; i < 11; i++)
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
                for (var i = 0; i < 6; i++)
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
                for (var t = GameStats.currentTurn; t >= 1; t--)
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
                for (var t = GameStats.currentTurn; t >= 1; t--)
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

            for (var i = 0; i < normalCardCount; i++)
                persons[i].personType = 2;
            //for (int i = normalCardCount; i < 6; i++)
            //    persons[i].personType = 3;
            persons[6].personType = ura_lmType == 0 ? 0 : 3;
            persons[7].personType = 4;
            persons[8].personType = 5;
            persons[9].personType = ura_tsyType == 0 ? 6 : 1;
            persons[10].personType = 7;

            if (ura_lmType == 0)
                headIdConvert[101] = 6;
            headIdConvert[102] = 7;
            headIdConvert[103] = 8;
            if (ura_tsyType == 0)
                headIdConvert[104] = 9;
            headIdConvert[2001] = 10;

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
                var p = headIdConvert[s.target_id];
                persons[p].friendship = s.evaluation;
            }
            //URA信息
            if (@event.data.chara_info.turn >= 3)
            {
                versus_level = @event.data.ura_data_set.versus_level;
                if (headIdConvert.ContainsValue(10)) 
                { 
                    var chara_id = @event.data.ura_data_set.evaluation_info_array.First(x => x.target_id == headIdConvert.First(x => x.Value == 10).Key)?.chara_id;
                    if (chara_id is not null)
                    {
                        var p = @event.data.ura_data_set.evaluation_info_array.First(x => x.chara_id == chara_id);
                        persons[10].member_state = p.member_state;
                    }
                 }
            }

            if (@event.data.home_info == null) return;
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
            foreach (var train in @event.data.ura_data_set.command_info_array)
            {
                //Console.WriteLine(train.command_id);
                if (!GameGlobal.ToTrainIndex.ContainsKey(train.command_id))//不是正常训练
                    continue;
                //Console.WriteLine("!");
                var versus_event_partner_id = train.versus_event_partner_id;
                if (versus_event_partner_id.HasValue){
                    versus_event = true;
                    break;
                }
            }
        }
    }
}
