using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos.Table;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    /// Conversation table entity
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to define the persistance model for an Azure Storage Table entity
    /// given the fact that Azure Storage Tables only support basic data types (strings, int, bool, etc.).
    /// 
    /// The idea is to provide support for the converstion to and from a fully typed object via the
    /// constructor and the ToObject() methods.  This allows the entity object to be express in basic
    /// data types but then be converted to and from a fully type object to program against. 
    /// </remarks>
    public class ConversationTableEntity : TableEntity
    {
        public static string CONVERSATION_TABLE_NAME = "Conversation";
        public static string CONVERSATION_PARTITION_KEY = "ConversationPartition";

        public ConversationTableEntity() { }

        // Need parameterless constructor for Table SDK deserialization to work when retrieving entities from table
        public ConversationTableEntity(Conversation conversation)
        {
            // Initialize values from conversation
            Update(conversation);
        }

        /// <summary>
        /// The thread Id of the ACS ChatThread
        /// </summary>
        /// <remarks>
        /// This property is the RowKey value of the TableEntity
        /// </remarks>
        public string ThreadId { get { return RowKey; } set { RowKey = value; } }

        public string EscalationContext { get; set; }

        public int Status { get; set; }

        /// <summary>
        /// List of agent Id's who are active in the conversation
        /// </summary>
        public string AgentId { get; set; }

        public string BotUser { get; set; }

        public int Disposition { get; set; }

        public int CStat { get; set; }

        public Conversation ToObject() { return new Conversation(this); }

        public void Update(Conversation conversation)
        {
            PartitionKey = CONVERSATION_PARTITION_KEY;
            ThreadId = conversation.ThreadId;
            //ConversationReference = JsonConvert.SerializeObject(conversation.ConversationReference);
            EscalationContext = JsonConvert.SerializeObject(conversation.EscalationContext);
            Status = (int)conversation.Status;
            AgentId = conversation.AgentId;
            Disposition = (int)conversation.Disposition;
            CStat = (int)conversation.CStat;
        }
    }

}
