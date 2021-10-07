using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    /// The custom context of the handoff from bot to AgentHub 
    /// </summary>
    /// <remarks>
    /// This class defines the context of the handoff from bot to AgentHub and is specific to the implementation
    /// of the AgentHub and the properties reflect that application-level feel to them (e.g., the WhyTheyNeedHelp
    /// property which is used in the agent-portal as a descriptor in the conversation panel.  Another agent-hub
    /// implementation might not use a descriptor at all or could have a whole separate set of context it needs
    /// to provide the agent experience it requires).  HandoffContext differes from EscalationContext in that its
    /// properties are specific to the Agent Hub/agent-portal whereas the EscalationContext properies are more 
    /// general and would apply to many if not all Agent Hubs.
    /// </remarks>
    public class HandoffContext
    {
        public HandoffContext() { }
        public HandoffContext(dynamic context)
        {
            Skill = context.Skill;
            Name = context.Name;
            CustomerType = context.CustomerType;
            WhyTheyNeedHelp = context.WhyTheyNeedHelp;
        }
        public string Skill { get; set; }
        public string Name { get; set; }
        public string CustomerType { get; set; }
        public string WhyTheyNeedHelp { get; set; }

        public bool IsValid() { return Skill != null && Name != null && CustomerType != null && WhyTheyNeedHelp != null; }
    }
}
