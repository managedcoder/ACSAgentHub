using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{

    public class ChatMessage
    {
        /// <summary>
        /// Default constructor that required to make deserialization work correctly (otherwise constructor with dynamic arg gets called)
        /// </summary>
        public ChatMessage() { }

        public ChatMessage(dynamic chatMessageObject)
        {
            messageBody = chatMessageObject.messageBody;
            messageId = chatMessageObject.messageId;
            type = chatMessageObject.type;
            version = chatMessageObject.version;
            senderDisplayName = chatMessageObject.senderDisplayName;
            senderCommunicationIdentifier = new Sendercommunicationidentifier()
            {
                communicationUser = new Communicationuser() { id = chatMessageObject.senderCommunicationIdentifier.communicationUser.id },
                rawId = chatMessageObject.senderCommunicationIdentifier.rawId
            };
            composeTime = chatMessageObject.composeTime;
            threadId = chatMessageObject.threadId;
            transactionId = chatMessageObject.transactionId;
        }

        public string messageBody { get; set; }
        public string messageId { get; set; }
        public string type { get; set; }
        public string version { get; set; }
        public string senderDisplayName { get; set; }
        public Sendercommunicationidentifier senderCommunicationIdentifier { get; set; }
        public DateTime composeTime { get; set; }
        public string threadId { get; set; }
        public string transactionId { get; set; }
    }

    public class Sendercommunicationidentifier
    {
        public string rawId { get; set; }
        public Communicationuser communicationUser { get; set; }
    }

    public class Communicationuser
    {
        public string id { get; set; }
    }

}
