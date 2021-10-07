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

namespace ACSAgentHub.APIs.Messages
{
    public class postMessageToAgent
    {
        //static private HttpClient _client;
        IConfiguration _config;
        public postMessageToAgent(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("postMessageToAgent")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "conversations/{acsThreadId}/messageToAgent")] HttpRequest req,
            ExecutionContext context,
            ILogger log,
            string acsThreadId)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                BotMessage message = JsonConvert.DeserializeObject<BotMessage>(requestBody);

                if (message != null)
                {
                    await ACSHelper.SendMessageToAgent(_config, acsThreadId, message.text);

                    return new OkResult();
                }
                else
                {
                    result = new ContentResult() { Content = $"Message object missing from body of {req.Method} to {req.Path.Value}", StatusCode = StatusCodes.Status400BadRequest };
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
