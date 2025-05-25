using Gallop;
using Spectre.Console;
using Newtonsoft.Json;

namespace UmamusumeResponseAnalyzer.AI
{
    public class TeamStadiumSend
    {
        public int[,,] proper;
        public int[,] style;
        public int[,][] attribute;
        public TeamStadiumSend(TeamStadiumOpponentListResponse @event)
        {
            proper = new int[3, 15, 3];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 15; j++)
                {
                    for (var k = 0; k < 3; k++)
                        proper[i, j, k] = -1;
                }
            }

            style = new int[3, 15];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 15; j++)
                    style[i, j] = -1;
            }

            attribute = new int[3, 15][];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 15; j++)
                    attribute[i, j] = [];
            }

            foreach (var opponent in @event.data.opponent_info_array)
            {
                foreach (var chara in opponent.team_data_array.Where(x => x.trained_chara_id != 0))
                {
                    var index = chara.distance_type * 3 + chara.member_id - 4;
                    style[opponent.strength - 1, index] = chara.running_style;
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
        public void doSend()
        {
            var currentGSdirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "GameData");
            Directory.CreateDirectory(currentGSdirectory);

            var success = false;
            var tried = 0;
            do
            {
                try
                {
                    var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }; // 去掉空值避免C++端抽风
                    File.WriteAllText($@"{currentGSdirectory}/TeamStadium.json", JsonConvert.SerializeObject(this, Formatting.Indented, settings));
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