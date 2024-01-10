using Gallop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.AI;

namespace UmamusumeResponseAnalyzer.Communications.Subscriptions
{
    public class SubscribeAiInfo_LArc(string wsKey) : BaseSubscription<GameStatusSend_LArc>(wsKey);
    public class SubscribeAiInfo_Ura(string wsKey) : BaseSubscription<GameStatusSend_Ura>(wsKey);
}
