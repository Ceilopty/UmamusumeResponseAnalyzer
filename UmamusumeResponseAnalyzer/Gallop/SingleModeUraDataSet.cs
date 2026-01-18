using Gallop;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gallop
{
    [MessagePackObject]
    public class SingleModeUraDataSet
    {
        [Key("command_info_array")]
        public SingleModeUraCommandInfo[] command_info_array; // 0x20
        [Key("evaluation_info_array")]
        public UraEvaluationInfo[] evaluation_info_array;
        [Key("versus_level")]
        public int versus_level;
    }
    [MessagePackObject]
    public class UraEvaluationInfo
    {
        [Key("target_id")]
        public int target_id; // 0x10
        [Key("chara_id")]
        public int chara_id; // 0x14
        [Key("member_state")]
        public int member_state;
    }
    [MessagePackObject]
    public class SingleModeUraCommandInfo
    {
        [Key("command_type")]
        public int command_type; // 0x10
        [Key("command_id")]
        public int command_id; // 0x14
        [Key("versus_event_partner_id")]
        public int? versus_event_partner_id; // 0x18
    }
}
