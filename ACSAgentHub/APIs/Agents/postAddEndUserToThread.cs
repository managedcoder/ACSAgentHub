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
    public class postAddEndUserToThread
    {
        IConfiguration _config;
        public postAddEndUserToThread(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("postAddEndUserToThread")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "AddEndUserToThread/{threadId}/{displayName}")] HttpRequest req,
            ILogger log,
            string threadId,
            string displayName)
        {
            await ACSHelper.AddEndUserToThread(_config, threadId, displayName);

            return new OkResult();
        }
    }
}
