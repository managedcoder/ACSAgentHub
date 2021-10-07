using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    public class BotEvent
    {
        public string ThreadId { get; set; }
        public ConversationStatus Status { get; set; }
    }
}
