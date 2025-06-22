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
    public class SingleModeTeamDataSet
    {
        [Key("command_info_array")]
        public SingleModeTeamCommandInfo[] command_info_array; // 0x20
        [Key("evaluation_info_array")]
        public TeamEvaluationInfo[] evaluation_info_array;
    }
    [MessagePackObject]
    public class TeamEvaluationInfo
    {
        [Key("target_id")]
        public int target_id; // 0x10
        [Key("chara_id")]
        public int chara_id; // 0x14
        [Key("member_state")]
        public int member_state;
        [Key("soul_threshold_id")]
        public int soul_threshold_id;
        [Key("soul_event_state")]
        public int soul_event_state;
    }
    [MessagePackObject]
    public class SingleModeTeamCommandInfo
    {
        [Key("command_type")]
        public int command_type; // 0x10
        [Key("command_id")]
        public int command_id; // 0x14
        [Key("params_inc_dec_info_array")]
        public SingleModeParamsIncDecInfo[] params_inc_dec_info_array; // 0x18
        [Key("guide_event_partner_array")]
        public int[] guide_event_partner_array;
        [Key("soul_event_partner_array")]
        public int[] soul_event_partner_array;
    }
}
