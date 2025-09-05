using Gallop;
using Spectre.Console;
using System.Linq;
using UmamusumeResponseAnalyzer.AI;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UmamusumeResponseAnalyzer.Handler
{
    public static partial class Handlers
    {
        public static void ParseCircleResponse(CircleResponse @event)
        {
            var circle = @event.data.circle_info;
            var name = circle.name;
            var member_num = circle.member_num;
            var comment = circle.comment;
            var circle_id = circle.circle_id;
            var make_time = circle.make_time;
            var policy = circle.policy switch
            {
                1 => "随意风格",
                2 => "休闲风格",
                3 => "硬核风格",
                4 => "新人可进",
                5 => "热闹风格",
                6 => "排行榜2000名以内",
                7 => "排行榜1000名以内",
                8 => "排行榜500名以内",
                9 => "排行榜250名以内",
                10 => "排行榜100名以内",
                11 => "排行榜20名以内",
                12 => "每天上线",
                13 => "2天内上线",
                14 => "常在早上游玩",
                15 => "常在白天游玩",
                16 => "常在晚上游玩",
                17 => "常在深夜游玩",
                _ => "未知"
            };
            var join_style = circle.join_style switch
            {
                1 => "自动同意",
                2 => "手动同意",
                3 => "仅限邀请",
            };
            AnsiConsole.MarkupLine($"[cyan]{name}[/] [green]({circle_id})[/] 人数 {member_num} 风格 {policy} 招新 {join_style}");
            AnsiConsole.MarkupLine($"创建时间 {make_time}");
            AnsiConsole.MarkupLine($"简介 {comment}");
            AnsiConsole.MarkupLine("------");
            AnsiConsole.MarkupLine("");
            var container = new Table
            {
                Border = TableBorder.Double
            };
            container.AddColumn(new TableColumn(string.Empty).NoWrap());
            container.HideHeaders();
            var table = new Table();
            table.Title("成员");
            var title = new List<string> { "职位", "昵称", "ID", "粉丝", "最后登录", "养成数", "JJC胜场", "JJC评分"};
            table.AddColumns(title.Select(e => new TableColumn(e).NoWrap()).ToArray());
            foreach (var i in @event.data.summary_user_info_array.OrderByDescending(x => x.last_login_time))
            {
                var memberline = new List<string> {};
                var membership = @event.data.circle_user_array.FirstOrDefault(x => x?.viewer_id == i.viewer_id, null)?.membership switch
                {
                    1 => "[green]成员[/]",
                    2 => "[aqua]副团长[/]",
                    3 => "[red]团长[/]",
                    _ => "前成员"
                };
                memberline.Add(membership);
                memberline.Add(i.name);
                memberline.Add(i.viewer_id.ToString());
                memberline.Add(i.fan.ToString());
                memberline.Add(i.last_login_time);
                memberline.Add(i.single_mode_play_count.ToString());
                memberline.Add(i.team_stadium_win_count.ToString());
                memberline.Add(i.team_evaluation_point.ToString());

                table.AddRow(memberline.ToArray());
            }
            container.AddRow(table);
            AnsiConsole.Write(container);
            try
            {
                var gameStatusToSend = new CircleSend(@event);
                if (Config.Get(Localization.Config.I18N_WriteAIInfo))
                    gameStatusToSend.doSend();
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]向AI发送数据失败！错误信息：{Environment.NewLine}{e.Message}[/]");
#if DEBUG
                throw;
#endif
            }
        }
    }
}
