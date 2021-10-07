using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    public class Agent
    {
        public Agent() { }

        public Agent(AgentTableEntity agentTableEntity)
        {
            // Initialize this instace with the values from agentTableEntity
            Update(agentTableEntity);
        }
        public string id { get; set; }
        public string name { get; set; }
        public AgentStatus status { get; set; }
        public List<string> skills { get; set; }

        public void Update(AgentTableEntity agentTableEntity)
        {
            id = agentTableEntity.id;
            name = agentTableEntity.name;
            status = (AgentStatus)agentTableEntity.status;
            skills = JsonConvert.DeserializeObject<List<string>>(agentTableEntity.skills);
        }
    }
}
