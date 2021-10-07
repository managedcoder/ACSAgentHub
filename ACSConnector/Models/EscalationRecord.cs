using System;
using System.Collections.Generic;
using System.Text;

namespace ACSConnector.Models
{
    /// <summary>
    /// Escalation record that holds key data related to an agent escalation
    /// </summary>
    /// <remarks>
    /// This class hold important ESCALATION data, not conversation data which is stored in an Azure Store
    /// Table.  We use the ThreadId of this class as a key to get the conversation data but the class 
    /// itself is about the escalation itself and is stored in Conversation State for access later.  Here's
    /// a bot more background...
    /// 
    /// When an agent escalation occurs, an EscalationRecord is created in order to save thread Id we'll
    /// need later to retrieve escalated conversation.  This escalation record is stored in conversation
    /// state as the breadcrumb to use to get the Conversation from the Azure Storage Table using ThreadId
    /// as the row key.  The Conversation record holds the state of the conversation and is used to broker
    /// messages from the bot to the agent hub
    /// </remarks>
    public class EscalationRecord
    {
        public string ThreadId { get; set; }
        public bool IsConversationEscalated { get { return ThreadId != null; } }
        public void EndEscalation() { ThreadId = null; }
    }
}
