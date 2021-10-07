using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    ///  The ACS User that represents all agents
    /// </summary>
    /// <remarks>
    /// ACSAgentUser is like a server-type of user so there is only one ACSAgentUser that is shared 
    /// across all ACSAgentUser instances. The name of the agent will be assigned later when 
    /// escalation happens.
    /// </remarks>
    public class ACSAgentUser : TableEntity
    {
        public static string AGENT_USER_TABLE_NAME = "AgentUser";
        public static string AGENT_USER_PARTITION_KEY = "AgentUserPartition";
        public static string AGENT_USER_ROW_KEY = "AgentUserRow";

        // Need parameterless constructor for Table SDK deserialization to work when retrieving entities from table
        public ACSAgentUser() { }

        public ACSAgentUser(string acsId)
        {
            this.PartitionKey = AGENT_USER_PARTITION_KEY;
            this.RowKey = AGENT_USER_ROW_KEY;
            ACS_Id = acsId;
        }

        public string ACS_Id { get; set; }
    }

}
