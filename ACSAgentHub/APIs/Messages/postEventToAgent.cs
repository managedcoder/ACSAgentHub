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
using ACSAgentHubSDK.Models;
using System.Net.Http;
using Azure.Messaging.WebPubSub;

namespace ACSAgentHub.APIs.Messages
{
    /// <summary>
    /// ToDo: Not using this API... get rid of it
    /// </summary>
    public class postEventToAgent
    {
        IConfiguration _config;
        public postEventToAgent(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("postEventToAgent")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "conversations/{acsThreadId}/eventToAgent")] HttpRequest req,
            ExecutionContext context,
            ILogger log,
            string acsThreadId)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                BotEvent conversationEvent = JsonConvert.DeserializeObject<BotEvent>(requestBody);

                if (conversationEvent != null)
                {
                    StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                    // Update the change of status in the conversation
                    ConversationTableEntity conversationTableEntity = await storageHelper.UpdateConversation(acsThreadId, conversationEvent.Status);

                    if (conversationTableEntity == null)
                    {
                        return new ContentResult() { Content = $"Update of conversation failed in {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status500InternalServerError };
                    }

                    if (conversationEvent.Status == ConversationStatus.Closed)
                    {
                        await ACSHelper.SendMessageToAgent(_config, acsThreadId, "The bot user has closed the conversation");

                        // Force other agent-hub instances to refresh their application context and effectively sync all clients with updated state
                        await ACSHelper.PublishRefreshEvent(_config);
                    }

                    return new OkResult();
                }
                else
                {
                    result = new ContentResult() { Content = $"BotEvent object missing from body of {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status400BadRequest };
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
