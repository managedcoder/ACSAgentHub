using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ACSAgentHubSDK.Models;
using ACSAgentHub.Utils;
using System.Net.Http;
using System.Net.Http.Json;

namespace ACSAgentHub.APIs.Conversations
{
    public class putConversation
    {
        IConfiguration _config;
        public putConversation(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("putConversation")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "conversations")] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Conversation conversation = JsonConvert.DeserializeObject<Conversation>(requestBody);

                if (conversation != null)
                {
                    StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);
                    string botBaseAddress = _config["botBaseAddress"].EndsWith('/') ? _config["botBaseAddress"] : _config["botBaseAddress"] + "/";

                    result = await storageHelper.UpdateConversation(conversation);

                    // Forward key conversation status changes to bot
                    if (conversation.Status == ConversationStatus.Accepted || conversation.Status == ConversationStatus.Closed)
                    {
                        var postRequest = new HttpRequestMessage(HttpMethod.Post, $"{botBaseAddress}api/ACSConnector/EventToBot/{conversation.ThreadId}")
                        {
                            Content = JsonContent.Create(conversation),
                            // ToDo: Set headers here when we add security to bot controller APIs
                        };

                        // Send agent hub event to bot
                        var postResponse = await Startup.httpClient.SendAsync(postRequest);

                        // Force other agent-hub instances to refresh their application context and effectively sync all clients with updated state
                        await ACSHelper.PublishRefreshEvent(_config, conversation.ThreadId);

                        postResponse.EnsureSuccessStatusCode();
                    }
                }
                else
                {
                    result = new ContentResult() { Content = $"Conversation object missing from body of {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status400BadRequest };
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
