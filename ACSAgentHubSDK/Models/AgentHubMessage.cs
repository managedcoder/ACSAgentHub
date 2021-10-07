using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    public class AgentHubMessage
    {
        public Conversation Conversation { get; set; }
        public ChatMessage ChatMessage { get; set; }
    }
}
