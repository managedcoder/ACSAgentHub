using System;
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
    public class putAgent
    {
        IConfiguration _config;
        public putAgent(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("putAgent")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "agents")] HttpRequest req,
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

                    result = await storageHelper.UpdateAgent(_config, agent);

                    // Force other agent-hub instances to refresh their application context and effectively sync all clients with updated state
                    await ACSHelper.PublishRefreshEvent(_config);
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
