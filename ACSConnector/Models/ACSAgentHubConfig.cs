using System;
using System.Collections.Generic;
using System.Text;

namespace ACSConnector.Models
{
    public class ACSAgentHubConfig
    {
        public const string Section = "acsAgentHubConfig";
        public string acsAgentHubBaseAddress { get; set; }
    }
}
