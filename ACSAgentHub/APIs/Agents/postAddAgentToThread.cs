using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ACSAgentHub.Utils;
using Microsoft.Extensions.Configuration;

namespace ACSAgentHub.APIs.Agents
{
    public class postAddAgentToThread
    {
        IConfiguration _config;
        public postAddAgentToThread(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("postAddAgentToThread")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "AddAgentToThread/{threadId}/{displayName}")] HttpRequest req,
            ILogger log,
            string threadId,
            string displayName)
        {
            await ACSHelper.AddAgentUserToThread(_config, threadId, displayName);

            return new OkResult();
        }
    }
}
