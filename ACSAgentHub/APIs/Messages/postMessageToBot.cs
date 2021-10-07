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
using System.Net.Http.Json;
using System.Text;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Azure;
using System.Net;
using ACSAgentHubSDK.Models;

namespace ACSAgentHub.APIs.Messages
{
    public class postMessageToBot
    {
        //static private HttpClient _client;
        IConfiguration _config;
        public postMessageToBot(IConfiguration config)
        {
            _config = config;
        }

        // Comment out until we get DI to work for HttpClient
        //static private HttpClient _client;

        //public postMessageToBot(HttpClient httpClient)
        //{
        //    _client = httpClient;
        //}

        /// <summary>
        /// Sends an agent message to the ACSConnector
        /// </summary>
        /// <param name="req"></param>
        /// <param name="context"></param>
        /// <param name="log"></param>
        /// <param name="acsThreadId"></param>
        /// <returns></returns>
        /// <remarks>
        /// This API acts as a pass-through to the ACSConnector which has access to the bot's messaging
        /// pipeline.  The chat message webhook points to this API and although we could have pointed
        /// that webhook directly to the ACSConnector, providing this passthrough shields the consumer
        /// from those implementation details and centralizes that logic and responsibility in the
        /// Agent Hub 
        /// </remarks>
        [FunctionName("postMessageToBot")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "agenthub/messageWebhook")] HttpRequest req,
            ILogger log)
        {
            IActionResult result = new OkResult();

            using (var readStream = new StreamReader(req.Body, Encoding.UTF8))
            {
                string requestContent = await readStream.ReadToEndAsync();

                try
                {
                    EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();

                    EventGridEvent[] eventGridEvents = eventGridSubscriber.DeserializeEventGridEvents(requestContent);

                    foreach (EventGridEvent eventGridEvent in eventGridEvents)
                    {
                        if (eventGridEvent.Data is SubscriptionValidationEventData)
                        {
                            var eventData = (SubscriptionValidationEventData)eventGridEvent.Data;
                            //log.LogInformation($"Got SubscriptionValidation event data, validation code: {eventData.ValidationCode}, topic: {eventGridEvent.Topic}");
                            // Do any additional validation (as required) and then return back the below response

                            var responseData = new SubscriptionValidationResponse()
                            {
                                ValidationResponse = eventData.ValidationCode
                            };

                            result = new OkObjectResult(responseData);
                        }
                        else if (eventGridEvent.EventType == "Microsoft.Communication.ChatMessageReceivedInThread")
                        {
                            ChatMessage chatMessage = new ChatMessage(eventGridEvent.Data);

                            await ACSHelper.SendMessageToBot(_config, chatMessage);
                        }
                    }
                }
                catch (Exception e)
                {
                    result = new ContentResult() { Content = $"Unexpected exception occurred in {req.Method} to {req.Path.Value}: {e.Message}", StatusCode = StatusCodes.Status500InternalServerError };
                }
            }

            return result;
        }
    }
}
