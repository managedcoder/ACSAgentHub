using System;
using System.Collections.Generic;
using System.Text;

namespace ACSConnector.Models
{

    public class ChatMessageReceivedInThread
    {
        public string messageBody { get; set; }
        public string messageId { get; set; }
        public string type { get; set; }
        public long version { get; set; }
        public string senderDisplayName { get; set; }
        public SenderCommunicationIdentifier senderCommunicationIdentifier { get; set; }
        public DateTime composeTime { get; set; }
        public string threadId { get; set; }
        public string transactionId { get; set; }
    }

    public class SenderCommunicationIdentifier
    {
        public string rawId { get; set; }
        public CommunicationUser communicationUser { get; set; }
    }

    public class CommunicationUser
    {
        public string id { get; set; }
    }
}
