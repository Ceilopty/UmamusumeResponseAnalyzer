using Gallop;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.AI;
using UmamusumeResponseAnalyzer.Communications.Subscriptions;

namespace UmamusumeResponseAnalyzer.Handler
{
    public static partial class Handlers
    {

        public static async void ParseTeamStadiumOpponentListResponse(Gallop.TeamStadiumOpponentListResponse @event)
        {
            var data = @event.data;
            var container = new Table
            {
                Border = TableBorder.Double
            };
            container.AddColumn(new TableColumn(string.Empty).NoWrap());
            container.HideHeaders();
            foreach (var i in data.opponent_info_array.OrderByDescending(x => -x.strength))
            {
                var Type = i.strength switch
                {
                    1 => "上",
                    2 => "中",
                    3 => "下"
                };
                var teamData = i.team_data_array.Where(x => x.trained_chara_id != 0).GroupBy(x => x.distance_type).ToDictionary(x => x.Key, x => x.ToList());
                var table = new Table();
                table.Title(Type);
                table.AddColumns(Enumerable.Repeat(new TableColumn("　　　").NoWrap(), 2 + teamData.Values.Sum(x=>x.Count)).ToArray());
                table.HideHeaders();
                var properTypeLine = new List<string> { string.Empty };
                var properValueLine = new List<string> { "适性" };
                var speedLine = new List<string> { "速度" };
                var staminaLine = new List<string> { "耐力" };
                var powerLine = new List<string> { "力量" };
                var gutsLine = new List<string> { "根性" };
                var wizLine = new List<string> { "智力" };
                int totalPower = 0;
                int totalWiz = 0;
                int charaNum = 0;
                foreach (var j in teamData)
                {
                    foreach (var k in j.Value)
                    {
                        var trainedChara = i.trained_chara_array.First(x => x.trained_chara_id == k.trained_chara_id);
                        var properType = string.Empty;
                        var properValue = string.Empty;
                        properType += (k.distance_type switch
                        {
                            5 => "泥",
                            _ => "芝"
                        });
                        properValue += (k.distance_type switch
                        {
                            5 => GetProper(trainedChara.proper_ground_dirt),
                            _ => GetProper(trainedChara.proper_ground_turf)
                        });
                        properValue += ' ';
                        properType += (k.distance_type switch
                        {
                            1 => "短",
                            2 => "英",
                            3 => "中",
                            4 => "长",
                            5 => "英"
                        });
                        properValue += (k.distance_type switch
                        {
                            1 => GetProper(trainedChara.proper_distance_short),
                            2 => GetProper(trainedChara.proper_distance_mile),
                            3 => GetProper(trainedChara.proper_distance_middle),
                            4 => GetProper(trainedChara.proper_distance_long),
                            5 => GetProper(trainedChara.proper_distance_mile)
                        });
                        properValue += ' ';
                        properType += (k.running_style switch
                        {
                            1 => "逃",
                            2 => "先",
                            3 => "差",
                            4 => "追"
                        });
                        properValue += (k.running_style switch
                        {
                            1 => GetProper(trainedChara.proper_running_style_nige),
                            2 => GetProper(trainedChara.proper_running_style_senko),
                            3 => GetProper(trainedChara.proper_running_style_sashi),
                            4 => GetProper(trainedChara.proper_running_style_oikomi)
                        });
                        properTypeLine.Add(properType);
                        properValueLine.Add(properValue);

                        charaNum += 1;

                        speedLine.Add(trainedChara.speed.ToString());
                        staminaLine.Add(trainedChara.stamina.ToString());

                        if (trainedChara.power < 600)
                            powerLine.Add("[green]" + trainedChara.power.ToString() + "[/]");
                        else if (trainedChara.power < 800)
                            powerLine.Add("[aqua]" + trainedChara.power.ToString() + "[/]");
                        else
                            powerLine.Add(trainedChara.power.ToString());
                        totalPower += trainedChara.power;

                        gutsLine.Add(trainedChara.guts.ToString());

                        if (trainedChara.wiz > 1600)
                            wizLine.Add("[aqua]" + trainedChara.wiz.ToString() + "[/]");
                        else
                            wizLine.Add(trainedChara.wiz.ToString());
                        totalWiz += trainedChara.wiz;



                    }
                }
                table.AddRow(properTypeLine.Append("平 均").ToArray());
                table.AddRow(properValueLine.Append("/ / /").ToArray());

                table.AddRow(speedLine.Append(speedLine.Skip(1).Average(x => int.Parse(x)).ToString("F0")).ToArray());
                table.AddRow(staminaLine.Append(staminaLine.Skip(1).Average(x => int.Parse(x)).ToString("F0")).ToArray());
                table.AddRow(powerLine.Append((totalPower/charaNum).ToString("F0")).ToArray());
                table.AddRow(gutsLine.Append(gutsLine.Skip(1).Average(x => int.Parse(x)).ToString("F0")).ToArray());
                table.AddRow(wizLine.Append((totalWiz / charaNum).ToString("F0")).ToArray());

                container.AddRow(table);
            }

            //设置宽度，Windows的CMD在大小<160时无法正常显示竞技场对手属性，会死循环
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (Console.BufferWidth < 160 || Console.WindowWidth < 160))
            {
                int[] x = [
                    Console.WindowWidth,
                    Console.LargestWindowWidth,
                    160];  // 破电脑1920宽138就溢出了，姑且改下。
                Console.BufferWidth = x.Order().ElementAt(1);
                Console.SetWindowSize(Console.BufferWidth, Console.WindowHeight);
            }
            AnsiConsole.Write(container);

            static string GetProper(int proper) => proper switch
            {
                1 => "G",
                2 => "F",
                3 => "E",
                4 => "D",
                5 => "C",
                6 => "B",
                7 => "A",
                8 => "S",
                _ => "错误"
            };
            try
            {
                var gameStatusToSend = new TeamStadiumSend(@event);
                if (Config.Get(Localization.Resource.ConfigSet_WriteAIInfo))
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
                            File.WriteAllText($@"{currentGSdirectory}/TeamStadium.json", JsonConvert.SerializeObject(gameStatusToSend, Formatting.Indented, settings));
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
                        AnsiConsole.MarkupLine($@"[red]写入{currentGSdirectory}/thisTurn.json失败！[/]");
                    }
                }
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
