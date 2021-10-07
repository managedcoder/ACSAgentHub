using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    public class Conversation
    {
        public Conversation() { }

        public Conversation(ConversationTableEntity conversationTableEntity)
        {
            ThreadId = conversationTableEntity.ThreadId;
            //if (conversationTableEntity.ConversationReference != null)
            //    ConversationReference = JsonConvert.DeserializeObject<ConversationReference>(conversationTableEntity.ConversationReference);
            EscalationContext = JsonConvert.DeserializeObject <EscalationContext>(conversationTableEntity.EscalationContext);
            Status = (ConversationStatus)conversationTableEntity.Status;
            AgentId = conversationTableEntity.AgentId;
            Disposition = (Disposition)conversationTableEntity.Disposition;
            TimeStamp = conversationTableEntity.Timestamp;
        }

        /// <summary>
        /// The thread Id of the ACS ChatThread
        /// </summary>
        public string ThreadId { get; set; }

        public EscalationContext EscalationContext { get; set; }

        public ConversationStatus Status { get; set; }

        /// <summary>
        /// List of agent Id's who are active in the conversation
        /// </summary>
        public string AgentId { get; set; }

        public Disposition Disposition { get; set; }

        public CStat CStat { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

}
