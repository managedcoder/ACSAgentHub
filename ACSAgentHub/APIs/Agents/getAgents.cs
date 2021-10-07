using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ACSAgentHub.Utils;
using ACSAgentHubSDK.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace ACSAgentHub.APIs.Agents
{
    public class getAgents
    {
        IConfiguration _config;
        public getAgents(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("getAgents")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "agents")]
            HttpRequest req,
            ILogger log)
        {
            IActionResult result = null;

            try
            {
                StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                List<Agent> agents = await storageHelper.GetAgents(_config);

                result = new OkObjectResult(agents); ;
            }
            catch (Exception e)
            {
                log.LogError($"HTTP {req.Method} on {req.Path.Value} failed: {e.Message}");

                result = new ContentResult() { Content = $"Unexpected exception occurred in {req.Method} to {req.Path.Value}: {e.Message}", StatusCode = StatusCodes.Status500InternalServerError };
            };

            return result;
        }
    }
}
