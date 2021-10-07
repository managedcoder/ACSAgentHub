using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    public class EscalationContext
    {
        public ConversationReference conversationReference { get; set; }

        public HandoffContext handoffContext { get; set; }

        public List<MinimizedTranscriptItem> transcript { get; set; }
    }
}
