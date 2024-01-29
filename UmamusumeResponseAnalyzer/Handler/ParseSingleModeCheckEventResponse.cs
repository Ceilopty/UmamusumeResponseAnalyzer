﻿using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.Entities;
using UmamusumeResponseAnalyzer.Localization;
using UmamusumeResponseAnalyzer.Game;
using UmamusumeResponseAnalyzer.AI;
using Newtonsoft.Json;

namespace UmamusumeResponseAnalyzer.Handler
{
    public static partial class Handlers
    {
        public static async void ParseSingleModeCheckEventResponse(Gallop.SingleModeCheckEventResponse @event)
        {
            // 这时当前事件还没有生效，先显示上一个事件的收益
            EventLogger.Update(@event);

            int eventCount = 0;
            foreach (var i in @event.data.unchecked_event_array)
            {
                eventCount += 1;
                if (GameStats.stats[GameStats.currentTurn] != null)
                {                    
                    if (i.story_id == 830137001)//第一次点击女神
                    {
                        GameStats.stats[GameStats.currentTurn].venus_isVenusCountConcerned = false;
                    }

                    if (i.story_id == 830137003)//女神三选一事件
                    {
                        GameStats.stats[GameStats.currentTurn].venus_venusEvent = true;
                    }


                    if (i.story_id == 400006112)//ss训练
                    {
                        GameStats.stats[GameStats.currentTurn].larc_playerChoiceSS = true;
                    }

                    if (i.story_id == 809043002)//佐岳启动
                    {
                        GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = 5;
                    }

                    if (i.story_id == 809043003)//佐岳充电
                    {
                        int suc = i.event_contents_info.choice_array[0].select_index;
                        int eventType = 0;
                        if (suc == 1)//加心情
                        {
                            eventType = 2;
                        }
                        else if (suc == 2)//不加心情
                        {
                            eventType = 1;
                        }

                        GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = eventType;
                    }
                    if (i.story_id == 400006115)//远征佐岳加pt
                    {
                        GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = 4;
                    }
                }

                //收录在数据库中
                if (Database.Events.TryGetValue(i.story_id, out var story))
                {
                    var mainTree = new Tree(story.TriggerName.EscapeMarkup()); //触发者名称
                    var eventTree = new Tree($"{story.Name.EscapeMarkup()}({i.story_id})"); //事件名称
                    for (var j = 0; j < i.event_contents_info.choice_array.Length; ++j)
                    {
                        var originalChoice = new Choice();
                        if (story.Choices.Count < (j + 1))
                        {
                            originalChoice.Option = "未知选项";
                            originalChoice.SuccessEffect = "未知效果";
                            originalChoice.FailedEffect = "未知效果";
                        }
                        else
                        {
                            originalChoice = story.Choices[j][0]; //因为kamigame的事件无法直接根据SelectIndex区分成功与否，所以必然只会有一个Choice;
                        }
                        //显示选项
                        var tree = new Tree($"{(string.IsNullOrEmpty(originalChoice.Option) ? Resource.SingleModeCheckEvent_Event_NoOption : originalChoice.Option)}{(Config.Get(Resource.ConfigSet_DisableSelectIndex) ? string.Empty : $" @ {i.event_contents_info.choice_array[j].select_index}")}".EscapeMarkup());
                        if (!Config.Get(Resource.ConfigSet_DisableSelectIndex) && Database.SuccessEvent.TryGetValue(i.story_id, out var successEvent) && successEvent.Choices.Length > j) //是可以成功的事件且已在数据库中
                            AddLoggedEvent(successEvent.Choices[j]);
                        else
                            AddNormalEvent();
                        eventTree.AddNode(tree);

                        void AddLoggedEvent(SuccessChoice[] choices)
                        {
                            var find = choices.WithSelectIndex(i.event_contents_info.choice_array[j].select_index)
                                .WithScenarioId(@event.data.chara_info.scenario_id)
                                .TryGet(out var choice);
                            if (find)
                                tree.AddNode(MarkupText(choice.Effect, choice.State));
                            else
                                tree.AddNode((string.IsNullOrEmpty(originalChoice.FailedEffect) ? originalChoice.SuccessEffect : MarkupText(originalChoice.FailedEffect, 0)));
                        }
                        void AddNormalEvent()
                        {
                            //如果没有失败效果则显示成功效果（别问我为什么这么设置，问kamigame
                            if (string.IsNullOrEmpty(originalChoice.FailedEffect))
                                tree.AddNode(originalChoice.SuccessEffect);
                            else if (originalChoice.SuccessEffect == "未知效果" && originalChoice.FailedEffect == "未知效果")
                                tree.AddNode(MarkupText($"未知效果", State.None));
                            else
                                tree.AddNode(MarkupText($"(成功时){originalChoice.SuccessEffect}{Environment.NewLine}(失败时){originalChoice.FailedEffect}{Environment.NewLine}", State.Fail));
                        }
                        string MarkupText(string text, State state)
                        {
                            return state switch
                            {
                                State.Unknown => $"[darkorange on #081129]{text}(不知道是否成功)[/]", //未知的
                                State.Fail => $"[#FF0050 on #081129]{text}[/]", //失败
                                State.Success => $"[mediumspringgreen on #081129]{text}[/]", //成功
                                State.GreatSuccess => $"[lightgoldenrod1 on #081129]{text}[/]", //大成功
                                State.None => $"[#afafaf on #081129]{text}[/]", //中性
                                _ => throw new NotImplementedException()
                            };
                        }
                    }
                    mainTree.AddNode(eventTree);
                    AnsiConsole.Write(mainTree);
                }
                else //未知事件，直接显示ChoiceIndex
                {
                    var mainTree = new Tree(Resource.SingleModeCheckEvent_Event_UnknownSource);
                    var eventTree = new Tree($"{Resource.SingleModeCheckEvent_Event_UnknownEvent}({i?.story_id})");
                    for (var j = 0; j < i?.event_contents_info?.choice_array.Length; ++j)
                    {
                        var tree = new Tree(string.Format(Resource.SingleModeCheckEvent_Event_UnknownOption, i.event_contents_info.choice_array[j].select_index));
                        tree.AddNode(Resource.SingleModeCheckEvent_Event_UnknownEffect);
                        eventTree.AddNode(tree);
                    }
                    mainTree.AddNode(eventTree);
                    AnsiConsole.Write(mainTree);

                }
                var eventToSend = new EventSend(@event, eventCount);
                if (Config.Get(Resource.ConfigSet_WriteEventInfo) && i?.event_contents_info?.choice_array.Length > 1)
                {
                    var currentGSdirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "GameData", "Event");
                    Directory.CreateDirectory(currentGSdirectory);

                    var success = false;
                    var tried = 0;
                    do
                    {
                        try
                        {
                            var settings = new JsonSerializerSettings
                            { 
                                NullValueHandling = NullValueHandling.Ignore,
                                Formatting = Formatting.Indented,
                            }; // 去掉空值避免C++端抽风
                            File.WriteAllText($@"{currentGSdirectory}/thisTurnThisEvent.json", JsonConvert.SerializeObject(eventToSend, settings));
                            File.WriteAllText($@"{currentGSdirectory}/turn{@event.data.chara_info.turn}Event{eventCount}.json", JsonConvert.SerializeObject(eventToSend, settings));
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
                if (@event.IsScenario(ScenarioType.Ura))
                {
                    try
                    {
                        var gameStatusToSend = new GameStatusSend_Ura(@event);
                        if (Config.Get(Localization.Resource.ConfigSet_WriteAIInfo))
                        {
                            var currentGSdirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "GameData", "Turn");
                            Directory.CreateDirectory(currentGSdirectory);

                            var success = false;
                            var tried = 0;
                            do
                            {
                                try
                                {
                                    var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }; // 去掉空值避免C++端抽风
                                    File.WriteAllText($@"{currentGSdirectory}/thisTurn.json", JsonConvert.SerializeObject(gameStatusToSend, Formatting.Indented, settings));
                                    File.WriteAllText($@"{currentGSdirectory}/turn{@event.data.chara_info.turn}.json", JsonConvert.SerializeObject(gameStatusToSend, Formatting.Indented, settings));
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
                } //if
            }
        }
    }
}
