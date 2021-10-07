using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using ACSAgentHub.Utils;
using System.Collections.Generic;
using ACSAgentHubSDK.Models;

namespace ACSAgentHub.APIs.Conversations
{
    public class getConversations
    {
        IConfiguration _config;
        public getConversations(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("getConversations")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "conversations")]
            HttpRequest req,
            ILogger log)
        {
            IActionResult result = null;

            try
            {

                StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                List<Conversation> conversations = await storageHelper.GetConversations();

                return new OkObjectResult(conversations);
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
