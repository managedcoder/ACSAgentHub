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
using System.Net.Http;
using System.Text;
using System.Net.Http.Json;
using ACSAgentHubSDK.Models;

namespace ACSAgentHub.APIs.Messages
{
    public class postEventToBot
    {
        //static private HttpClient _client;
        IConfiguration _config;
        public postEventToBot(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Sends an agent event to the Agent Hub
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        /// <remarks>
        /// Expects acsThreadId as part of the API URL and ConversationStatus in the body
        /// </remarks>
        [FunctionName("postEventToBot")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agenthub/{acsThreadId}/eventToBot")] HttpRequest req,
            ExecutionContext context,
            ILogger log,
            string acsThreadId)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ConversationStatus? conversationEvent = JsonConvert.DeserializeObject<ConversationStatus>(requestBody);

                if (conversationEvent != null)
                {
                    StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                    // Update the change of status in the conversation
                    ConversationTableEntity conversationTableEntity = await storageHelper.UpdateConversation(acsThreadId, conversationEvent.Value);

                    if (conversationTableEntity == null)
                    {
                        return new ContentResult() { Content = $"Update of conversation failed in {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status500InternalServerError };
                    }

                    string botBaseAddress = _config["botBaseAddress"].EndsWith('/') ? _config["botBaseAddress"] : _config["botBaseAddress"] + "/";
                    var postRequest = new HttpRequestMessage(HttpMethod.Post, $"{botBaseAddress}api/ACSConnector/EventToBot/{acsThreadId}")
                    {
                        Content = JsonContent.Create(new Conversation(conversationTableEntity)),
                        // ToDo: Set headers here when we add security to bot controller APIs
                    };

                    // Send agent hub event to bot
                    var postResponse = await Startup.httpClient.SendAsync(postRequest);

                    postResponse.EnsureSuccessStatusCode();

                    return new OkResult();
                }
                else
                {
                    result = new ContentResult() { Content = $"Event object missing from body of {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status400BadRequest };
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
