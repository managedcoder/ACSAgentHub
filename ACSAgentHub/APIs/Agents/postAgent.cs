using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
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
    public class postAgent
    {
        IConfiguration _config;
        public postAgent(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("postAgent")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agents")] HttpRequest req,
            ILogger log)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Agent agent = JsonConvert.DeserializeObject<Agent>(requestBody);

                if (agent != null)
                {
                    StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                    await storageHelper.AddToAgents(_config, agent);

                    result = new OkResult();
                }
                else
                {
                    result = new ContentResult() { Content = $"Agent object missing from body of {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status400BadRequest };
                }
            }
            catch (Exception e)
            {
                log.LogError($"HTTP {req.Method} on {req.Path.Value} failed: {e.Message}");

                result = new ContentResult() { Content = $"Unexpected exception occurred in {req.Method} to {req.Path.Value}: {e.Message}", StatusCode = StatusCodes.Status500InternalServerError };
            }

            return result;
        }
    }

}
