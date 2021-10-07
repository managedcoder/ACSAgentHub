using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    /// The Agent table entity
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to define the persistance model for an Azure Storage Table entity
    /// given the fact that Azure Storage Tables only support basic data types (strings, int, bool, etc.).
    /// 
    /// The idea is to provide support for the converstion to and from a fully typed object via the
    /// constructor and the ToObject() methods.  This allows the entity object to be express in basic
    /// data types but then be converted to and from a fully type object to program against. 
    /// </remarks>
    public class AgentTableEntity : TableEntity
    {
        public static string AGENT_TABLE_NAME = "Agent";
        public static string AGENT_PARTITION_KEY = "AgentPartition";

        public AgentTableEntity() { }

        // Need parameterless constructor for Table SDK deserialization to work when retrieving entities from table
        public AgentTableEntity(Agent agent)
        {
            // Initialize values from agent
            Update(agent);
        }

        public string id { get { return RowKey; } set { RowKey = value; } }
        public string name { get; set; }
        public int status { get; set; }
        public string skills { get; set; }

        public void Update(Agent agent)
        {
            PartitionKey = AGENT_PARTITION_KEY;
            id = agent.id;
            name = agent.name;
            status = (int)agent.status;
            skills = JsonConvert.SerializeObject(agent.skills);
        }
    }

}
