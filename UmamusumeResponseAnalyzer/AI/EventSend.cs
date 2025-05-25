using UmamusumeResponseAnalyzer.Entities;
using Gallop;
using Spectre.Console;
using Newtonsoft.Json;

namespace UmamusumeResponseAnalyzer.AI
{
    public class EventSend
    {
        public bool islegal;//是否为有效的事件数据
        public int turn;//当前回合，用于文件名
        public int eventCount;//事件计次，用于文件名
        public string triggerName;//触发者名称
        public string eventName;//事件名称
        public int story_id;//事件编号
        public string[] choices;//选项
        public int[] select_indices;//隐藏
        public State[] is_success;//是否成功 -1未知 0失败 1成功 2大成功 max中性
        public string[] effect;//效果

        public SingleModeEventInfo eventInfo; //原始数据
        public EventSend(SingleModeCheckEventResponse @event, int index)
        {
            islegal = false;
            turn = @event.data.chara_info.turn;
            eventCount = index;
            eventInfo = @event.data.unchecked_event_array[index - 1];
            var length = eventInfo.event_contents_info.choice_array.Length;
            choices = new string[length];
            select_indices = new int[length];
            is_success = new State[length];
            effect = new string[length];
            //收录在数据库中
            if (Database.Events.TryGetValue(eventInfo.story_id, out var story))
            {
                triggerName = story.TriggerName;
                eventName = story.Name;
                story_id = eventInfo.story_id;
                for (var j = 0; j < length; ++j)
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
                    choices[j] = string.IsNullOrEmpty(originalChoice.Option) ? "无选项" : originalChoice.Option;
                    select_indices[j] = eventInfo.event_contents_info.choice_array[j].select_index;
                    if (Database.SuccessEvent.TryGetValue(eventInfo.story_id, out var successEvent) && successEvent.Choices.Length > j) //是可以成功的事件且已在数据库中
                        AddLoggedEvent(successEvent.Choices[j]);
                    else
                        AddNormalEvent();
                    void AddLoggedEvent(SuccessChoice[] choices)
                    {
                        var find = choices.WithSelectIndex(eventInfo.event_contents_info.choice_array[j].select_index)
                            .WithScenarioId(@event.data.chara_info.scenario_id)
                            .TryGet(out var choice);
                        if (find)
                        {
                            effect[j] = choice.Effect;
                            is_success[j] = choice.State;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(originalChoice.FailedEffect))
                            {
                                effect[j] = originalChoice.SuccessEffect;
                                is_success[j] = State.None;
                            }
                            else
                            {
                                effect[j] = originalChoice.FailedEffect;
                                is_success[j] = State.Fail;
                            }
                        }
                    }
                    void AddNormalEvent()
                    {
                        //如果没有失败效果则显示成功效果（别问我为什么这么设置，问kamigame
                        if (string.IsNullOrEmpty(originalChoice.FailedEffect))
                        {
                            effect[j] = originalChoice.SuccessEffect;
                            is_success[j] = State.None;
                        }
                        else if (originalChoice.SuccessEffect == "未知效果" && originalChoice.FailedEffect == "未知效果")
                        {
                            effect[j] = "未知效果";
                            is_success[j] = State.None;
                        }
                        else
                        {
                            effect[j] = $"(成功时){originalChoice.SuccessEffect}{Environment.NewLine}(失败时){originalChoice.FailedEffect}";
                            is_success[j] = State.Fail;
                        }
                    }
                }
            }
            else //未知事件，直接显示ChoiceIndex
            {
                triggerName = "未知来源";
                eventName = "未知事件";
                story_id = eventInfo != null ? eventInfo.story_id : 0;
                for (var j = 0; j < length; ++j)
                {
                    choices[j] = "未知选项";
                    select_indices[j] = eventInfo.event_contents_info.choice_array[j].select_index;
                    is_success[j] = State.Unknown;
                    effect[j] = "未知效果";
                }
            }
            islegal = true;
        }
        public void doSend()
        {
            if (this.islegal == false)
            {
                return;
            }
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
                    File.WriteAllText($@"{currentGSdirectory}/thisTurnThisEvent.json", JsonConvert.SerializeObject(this, settings));
                    File.WriteAllText($@"{currentGSdirectory}/turn{this.turn}Event{eventCount}.json", JsonConvert.SerializeObject(this, settings));
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