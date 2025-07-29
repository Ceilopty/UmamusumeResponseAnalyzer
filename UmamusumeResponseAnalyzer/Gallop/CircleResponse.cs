using MessagePack;

namespace Gallop
{
    [MessagePackObject]
    public sealed class CircleResponse : ResponseCommon
    {
        [Key("data")]
        public CommonResponse data;

        [MessagePackObject]
        public class CommonResponse
        {
            [Key("circle_info")]
            public CircleInfo circle_info;
            [Key("circle_user_array")]
            public CircleUser[] circle_user_array;
            [Key("summary_user_info_array")]
            public SummaryUserInfo[] summary_user_info_array;
        }

        [MessagePackObject]
        public class CircleInfo: CircleInfoAtUser
        {
            [Key("leader_viewer_id")]
            public long leader_viewer_id;
            [Key("comment")]
            public string comment;
            [Key("member_num")]
            public int member_num;
            [Key("join_style")]
            public int join_style;
            [Key("policy")]
            public int policy;
            [Key("make_time")]
            public string make_time;
            [Key("all_silence_status")]
            public int all_silence_status;
            [Key("name_freeze_status")]
            public int name_freeze_status;
            [Key("comment_freeze_status")]
            public int comment_freeze_status;
        }
        [MessagePackObject]
        public class SummaryUserInfo
        {
            [Key("viewer_id")]
            public long viewer_id;
            [Key("name")]
            public string name;
            [Key("honor_id")]
            public int honor_id;
            [Key("last_login_time")]
            public string last_login_time;
            [Key("leader_chara_id")]
            public int leader_chara_id;
            [Key("leader_chara_dress_id")]
            public int leader_chara_dress_id;
            [Key("support_card_id")]
            public int support_card_id;
            [Key("partner_chara_id")]
            public int partner_chara_id;
            [Key("comment")]
            public string comment;
            [Key("fan")]
            public long fan;
            [Key("rank_score")]
            public int rank_score;
            [Key("team_stadium_win_count")]
            public int team_stadium_win_count;
            [Key("single_mode_play_count")]
            public int single_mode_play_count;
            [Key("team_evaluation_point")]
            public int team_evaluation_point;
            [Key("user_support_card")]
            public UserSupportCardAtCircle user_support_card;
            [Key("user_trained_chara")]
            public UserTrainedCharaAtCircle user_trained_chara;
            [Key("circle_info")]
            public CircleInfoAtUser circle_info;
            [Key("circle_user")]
            public CircleUser circle_user;
            [Key("friend_state")]
            public int friend_state;
        }
        [MessagePackObject]
        public class UserSupportCardAtCircle // TypeDefIndex: 7586
        {
            [Key("support_card_id")]
            public int support_card_id; // 0x10
            [Key("exp")]
            public int exp; // 0x14
            [Key("limit_break_count")]
            public int limit_break_count;
        }
        [MessagePackObject]
        public class UserTrainedCharaAtCircle // TypeDefIndex: 7589
        {
            [Key("viewer_id")]
            public long viewer_id; // 0x10
            [Key("trained_chara_id")]
            public int trained_chara_id; // 0x18
            [Key("card_id")]
            public int card_id; // 0x1C
            [Key("rank_score")]
            public int rank_score; // 0x20
            [Key("rank")]
            public int rank; // 0x24
            [Key("proper_distance_short")]
            public int proper_distance_short; // 0x28
            [Key("proper_distance_mile")]
            public int proper_distance_mile; // 0x2C
            [Key("proper_distance_middle")]
            public int proper_distance_middle; // 0x30
            [Key("proper_distance_long")]
            public int proper_distance_long; // 0x34
            [Key("proper_running_style_nige")]
            public int proper_running_style_nige; // 0x38
            [Key("proper_running_style_senko")]
            public int proper_running_style_senko; // 0x3C
            [Key("proper_running_style_sashi")]
            public int proper_running_style_sashi; // 0x40
            [Key("proper_running_style_oikomi")]
            public int proper_running_style_oikomi; // 0x44
            [Key("proper_ground_turf")]
            public int proper_ground_turf; // 0x48
            [Key("proper_ground_dirt")]
            public int proper_ground_dirt; // 0x4C
            [Key("rarity")]
            public int rarity; // 0x50
            [Key("talent_level")]
            public int talent_level; // 0x54
            [Key("register_time")]
            public string register_time; // 0x58
            [Key("factor_id_array")]
            public int[] factor_id_array; // 0x60
            [Key("factor_info_array")]
            public FactorInfo[] factor_info_array; // 0x68
            [Key("skill_count")]
            public int skill_count; // 0x70
        }
        [MessagePackObject]
        public class CircleInfoAtUser
        {
            [Key("circle_id")]
            public long circle_id;
            [Key("name")]
            public string name;
        }
    }
}
