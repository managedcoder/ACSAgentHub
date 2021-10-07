using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    public class AgentHubEvent
    {
        /// <summary>
        /// The Conversation record
        /// </summary>
        public Conversation Conversation { get; set; }
        /// <summary>
        /// The change in status that triggered this event
        /// </summary>
        public ConversationStatus Status { get; set; }
    }
}
