using Gallop;
using Spectre.Console;
using Newtonsoft.Json;
using UmamusumeResponseAnalyzer.Game;

namespace UmamusumeResponseAnalyzer.AI
{
    public class CircleMember
    {
        public long viewer_id;
        public string name;
        public string last_login_time;
        public string comment;
        public long fan;
        public int team_stadium_win_count;
        public int single_mode_play_count;
        public int team_evaluation_point;
        public CircleMember()
        {
            viewer_id = -1;
            name = "";
            last_login_time = "";
            comment = "";
            fan = 0;
            team_stadium_win_count = 0;
            single_mode_play_count = 0;
            team_evaluation_point = 0;
        }
    }
    public class CircleSend
    {
        public long circle_id;
        public string name;
        public int member_num;
        public CircleMember[] members;
        public CircleSend(CircleResponse @event)
        {
            circle_id = @event.data.circle_info.circle_id;
            name = @event.data.circle_info.name;
            member_num = @event.data.circle_info.member_num;
            members = new CircleMember[member_num];
            for (var i = 0; i < member_num; i++)
            {
                var user = @event.data.summary_user_info_array[i];
                members[i] = new CircleMember
                {
                    viewer_id = user.viewer_id,
                    name = user.name,
                    last_login_time = user.last_login_time,
                    comment = user.comment,
                    fan = user.fan,
                    team_stadium_win_count = user.team_stadium_win_count,
                    single_mode_play_count = user.single_mode_play_count,
                    team_evaluation_point = user.team_evaluation_point
                };
            }
        }
        public async void doSend()
        {
            var currentGSdirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "GameData", "Circle");
            Directory.CreateDirectory(currentGSdirectory);

            var success = false;
            var tried = 0;
            do
            {
                try
                {
                    var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }; // 去掉空值避免C++端抽风
                    File.WriteAllText($@"{currentGSdirectory}/Circle.json", JsonConvert.SerializeObject(this, Formatting.Indented, settings));
                    File.WriteAllText($@"{currentGSdirectory}/Circle_{DateTime.Now:yy-MM-dd HH-mm-ss-fff}.json", JsonConvert.SerializeObject(this, Formatting.Indented, settings));
                    success = true; // 写入成功，跳出循环
                    break;
                }
                catch
                {
                    tried++;
                    AnsiConsole.MarkupLine("[yellow]写入失败，0.5秒后重试...[/]");
                    await Task.Delay(500); // 等待0.5秒
                }
            } while (!success && tried < 10);
            if (!success)
            {
                AnsiConsole.MarkupLine($@"[red]写入{currentGSdirectory}/Circle.json失败！[/]");
            }
        }
    }
}