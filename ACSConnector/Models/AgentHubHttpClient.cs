using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace ACSConnector.Models
{
    public class AgentHubHttpClient
    {
        public AgentHubHttpClient(HttpClient client)
        {
            Client = client;
        }

        public HttpClient Client { get; }
    }
}
