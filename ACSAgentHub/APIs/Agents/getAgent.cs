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
using Newtonsoft.Json;

namespace ACSAgentHub.APIs.Agents
{
    public class getAgent
    {
        IConfiguration _config;
        public getAgent(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("getAgent")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "agents/{agentId}")] HttpRequest req,
            ILogger log,
            string agentId)
        {
            IActionResult result = null;

            try
            {
                StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                Agent agent = await storageHelper.GetAgent(_config, agentId);

                result = new OkObjectResult(agent);
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
