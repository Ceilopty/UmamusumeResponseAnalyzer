using Gallop;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.AI;
using static UmamusumeResponseAnalyzer.Localization.Game;
using static UmamusumeResponseAnalyzer.LocalizedLayouts.Handlers.ParseTeamStadiumOpponentListResponse;
using Newtonsoft.Json;

namespace UmamusumeResponseAnalyzer.Handler
{
    public static partial class Handlers
    {

        public static void ParseTeamStadiumOpponentListResponse(Gallop.TeamStadiumOpponentListResponse @event)
        {
            
            var data = @event.data;
            if (data.opponent_info_array != null)
            {
                // 3个是选人之前。由于trained_chara_array无了，读取对手账号信息
                foreach (var i in data.opponent_info_array)
                {
                    var str = i.strength;
                    var user = i.user_info;
                    var name = user.name;
                    var play_count = user.single_mode_play_count;
                    var day_count = user.total_login_day_count;
                    AnsiConsole.MarkupLine($"#{str}: [cyan]{name}[/] 登陆日数 {day_count} 育成数 {play_count} ([cyan]{((double)play_count / day_count).ToString("N1")}[/]/日)");
                    AnsiConsole.MarkupLine("------");
                }
                AnsiConsole.MarkupLine("");
            }
            else if (data.opponent_info_copy != null)
            {
                // 1个是选人之后，仍能看到属性
                var team = data.opponent_info_copy.team_data_array;
                var trained = data.opponent_info_copy.trained_chara_array;
                var name = data.opponent_info_copy.user_info.name;
                var distStats = new Dictionary<string, int>();
                var groundStats = new Dictionary<string, int>();
                var styleStats = new Dictionary<string, int>();

                var teamData = team.Where(x => x.trained_chara_id != 0).GroupBy(x => x.distance_type).ToDictionary(x => x.Key, x => x.ToList());
                foreach (var j in teamData)
                {
                    foreach (var k in j.Value)
                    {
                        var trainedChara = trained.FirstOrDefault(x => x.trained_chara_id == k.trained_chara_id);
                        if (trainedChara == null) continue;

                        // 场地适性
                        // 判断是否泥地
                        var groundType = k.distance_type switch
                        {
                            5 => I18N_Dirt,
                            _ => I18N_Grass
                        };
                        var groundProper = k.distance_type switch
                        {
                            5 => GetProper(trainedChara.proper_ground_dirt),
                            _ => GetProper(trainedChara.proper_ground_turf)
                        };
                        // 距离适性
                        var distType = k.distance_type switch
                        {
                            1 => I18N_Short,
                            2 => I18N_Mile,
                            3 => I18N_Middle,
                            4 => I18N_Long,
                            5 => I18N_Mile
                        };
                        var distProper = (k.distance_type switch
                        {
                            1 => GetProper(trainedChara.proper_distance_short),
                            2 => GetProper(trainedChara.proper_distance_mile),
                            3 => GetProper(trainedChara.proper_distance_middle),
                            4 => GetProper(trainedChara.proper_distance_long),
                            5 => GetProper(trainedChara.proper_distance_mile)
                        });
                        // 跑法适性
                        var styleType = k.running_style switch
                        {
                            1 => I18N_Nige,
                            2 => I18N_Senko,
                            3 => I18N_Sashi,
                            4 => I18N_Oikomi
                        };
                        var styleProper = k.running_style switch
                        {
                            1 => GetProper(trainedChara.proper_running_style_nige),
                            2 => GetProper(trainedChara.proper_running_style_senko),
                            3 => GetProper(trainedChara.proper_running_style_sashi),
                            4 => GetProper(trainedChara.proper_running_style_oikomi)
                        };
                        // 统计
                        if (!distStats.ContainsKey(distProper)) distStats[distProper] = 0;
                        distStats[distProper] += 1;
                        if (!groundStats.ContainsKey(groundProper)) groundStats[groundProper] = 0;
                        groundStats[groundProper] += 1;
                        if (!styleStats.ContainsKey(styleProper)) styleStats[styleProper] = 0;
                        styleStats[styleProper] += 1;
                    }
                } // foreach
                AnsiConsole.MarkupLine($"当前对手: [cyan]{name}[/]");
                AnsiConsole.MarkupLine($"距离适性: [cyan]{JsonConvert.SerializeObject(distStats)}[/]");
                AnsiConsole.MarkupLine($"场地适性: {JsonConvert.SerializeObject(groundStats)}");
                AnsiConsole.MarkupLine($"跑法适性: {JsonConvert.SerializeObject(styleStats)}");
                AnsiConsole.MarkupLine($"----");
                AnsiConsole.MarkupLine($"");
            }

            #region 老版还有这些
            if (data.opponent_info_array != null)
            {
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
                    table.AddColumns(Enumerable.Repeat(new TableColumn("　　　").NoWrap(), 2 + teamData.Values.Sum(x => x.Count)).ToArray());
                    table.HideHeaders();
                    var properTypeLine = new List<string> { string.Empty };
                    var properValueLine = new List<string> { "适性" };
                    var speedLine = new List<string> { "速度" };
                    var staminaLine = new List<string> { "耐力" };
                    var powerLine = new List<string> { "力量" };
                    var gutsLine = new List<string> { "根性" };
                    var wizLine = new List<string> { "智力" };
                    var totalPower = 0;
                    var totalWiz = 0;
                    var charaNum = 0;
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
                    table.AddRow(powerLine.Append((totalPower / charaNum).ToString("F0")).ToArray());
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
            }
            #endregion
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
                _ => throw new NotImplementedException()
            };
            // 发送UAT所需JJC信息
            if (data.opponent_info_array != null)
            {
                try
                {
                    var gameStatusToSend = new TeamStadiumSend(@event);
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
}
