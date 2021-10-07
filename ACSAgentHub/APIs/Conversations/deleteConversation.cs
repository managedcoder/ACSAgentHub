using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using ACSAgentHub.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using ACSAgentHubSDK.Models;

namespace ACSAgentHub.APIs.Conversations
{
    public class DeleteConversation
    {
        IConfiguration _config;
        public DeleteConversation(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("DeleteConversation")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "conversations/{acsThreadId}")] HttpRequest req,
            ExecutionContext context,
            ILogger log,
            string acsThreadId)
        {
            IActionResult result = new OkResult();

            ///
            /// An ACS Agent Hub conversation consists of conversation record in Azure Storage and a corresponding
            /// ACS Thread that hosts the conversation between bot user and agent.  To delete a conversation you
            /// need to delete both record and thread.  Its not an error to attempt to delete a non-existant
            /// record or thread.
            ///

            try
            {
                // Delete the conversation record in Azure Storage
                int rc = await DeleteConversationRecord(_config, acsThreadId);

                // If conversation wasn't already closed/deleted by another agent, then also delete the ACS thread
                if (rc == StatusCodes.Status200OK)
                {
                    // Delete the conversation thread in ACS
                    await DeleteConversationThread(_config, acsThreadId);
                }

                // Force other agent-hub instances to refresh their application context and effectively sync all clients with updated state
                await ACSHelper.PublishRefreshEvent(_config, acsThreadId);
            }
            catch (Exception e)
            {
                log.LogError($"HTTP {req.Method} on {req.Path.Value} failed: {e.Message}");

                result = new ContentResult() { Content = $"Unexpected exception occurred in {req.Method} to {req.Path.Value}: {e.Message}", StatusCode = StatusCodes.Status500InternalServerError };
            };

            return result;
        }

        async static Task<int> DeleteConversationRecord(
            IConfiguration config,
            string acsThreadId)
        {
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

            ConversationTableEntity conversation = await storageHelper.DeleteConversationRecord(acsThreadId);

            if (conversation == null)
                return StatusCodes.Status404NotFound;
            else
                return StatusCodes.Status200OK;
        }


        async Task<IActionResult> DeleteConversationThread(
            IConfiguration config,
            string acsThreadId)
        {
            await ACSHelper.DeleteEscalationThread(_config, acsThreadId);

            return new StatusCodeResult(StatusCodes.Status200OK);
        }
    }
}
