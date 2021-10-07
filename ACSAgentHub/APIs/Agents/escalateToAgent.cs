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
using Azure.Communication.Chat;
using ACSAgentHubSDK.Models;
using Azure.Messaging.WebPubSub;

namespace ACSAgentHub.APIs.Agents
{
    public class escalateToAgent
    {
        IConfiguration _config;
        public escalateToAgent(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("escalateToAgent")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            IActionResult result = null;

            try
            {
                result = await EscalateToAgent(req, _config, log);

                // Force other agent-hub instances to refresh their application context and effectively sync all clients with updated state
                await ACSHelper.PublishRefreshEvent(_config);
            }
            catch (Exception e)
            {
                log.LogError($"HTTP {req.Method} on {req.Path.Value} failed: {e.Message}");

                result = new ContentResult() { Content = $"Unexpected exception occurred in {req.Method} to {req.Path.Value}: {e.Message}", StatusCode = StatusCodes.Status500InternalServerError };
            }

            return result;
        }

        async Task<IActionResult> EscalateToAgent(HttpRequest req,
         IConfiguration config,
         ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            EscalationContext escalationContext = JsonConvert.DeserializeObject<EscalationContext>(requestBody);

            if (escalationContext == null)
            {
                return new ContentResult() { Content = $"The EscalationContext object in the body of {req.Method} to {req.Path.Value} was missing", StatusCode = StatusCodes.Status400BadRequest };
            }

            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

            log.LogInformation($"In {req.Method} method of {req.Path.Value} - name: {escalationContext.handoffContext.Name}");

            ChatThreadClient chatThreadClient = await ACSHelper.CreateEscalationThread(_config, escalationContext);

            log.LogInformation($"{escalationContext.handoffContext.Name} is escalating to agent on new thread: {chatThreadClient.Id}");

            await storageHelper.AddToConversations(new Conversation() {ThreadId = chatThreadClient.Id, EscalationContext = escalationContext });

            return new OkObjectResult(chatThreadClient.Id);
        }

    }
}
