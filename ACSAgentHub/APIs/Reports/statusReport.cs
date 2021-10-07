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
using Azure.Communication;
using Azure;
using System.Collections.Generic;
using ACSAgentHubSDK.Models;
using Microsoft.Azure.Cosmos.Table;

namespace ACSAgentHub.APIs.Reports
{
    public class statusReport
    {
        //static private HttpClient _client;
        IConfiguration _config;
        public statusReport(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("statusReport")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            IActionResult result = new OkResult();

            try
            {
                string name = req.Query["name"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.name;

                string responseMessage = string.IsNullOrEmpty(name)
                    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                    : $"Hello, {name}. This HTTP triggered function executed successfully.";

                var userAndToken = await ACSHelper.GetACSAgentUserAndAccessToken(_config);

                ChatClient chatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(_config["acsConnectionString"])), new CommunicationTokenCredential(userAndToken.token));
                StorageHelper storageHelper = new StorageHelper(_config["agentHubStorageConnectionString"]);

                string conversationSummary = await LogConversationSummary(_config, chatClient, storageHelper);

                result = new OkObjectResult(conversationSummary);
            }
            catch (Exception e)
            {
                log.LogError($"HTTP {req.Method} on {req.Path.Value} failed: {e.Message}");

                result = new ContentResult() { Content = $"Unexpected exception occurred in {req.Method} to {req.Path.Value}: {e.Message}", StatusCode = StatusCodes.Status500InternalServerError };
            };

            return result;
        }

        ///
        /// Private class methods
        /// 

        async Task<string> LogConversationSummary(IConfiguration config, ChatClient chatClient, StorageHelper storageHelper)
        {
            string summary = null;

            var acsThreadSummaryResult = await GetACSThreadSummary(chatClient);

            var agentConversationSummaryResult = await GetAgentConversationSummary(config, acsThreadSummaryResult.acsThreads);

            string orphandAgentConverationSummary = GetOrphandAgentConverationSummary(agentConversationSummaryResult.agentConversations);

            string orphandACSThreadSummary = GetOrphandACSThreadSummary(acsThreadSummaryResult.acsThreads);

            string agentSummary = await GetAgentSummary(_config);

            summary = acsThreadSummaryResult.acsThreadSummary + agentConversationSummaryResult.agentConversationSummary + orphandACSThreadSummary + orphandAgentConverationSummary + agentSummary;

            return summary;
        }

        async private static Task<(string acsThreadSummary, List<string> acsThreads)> GetACSThreadSummary(ChatClient chatClient)
        {
            AsyncPageable<ChatThreadItem> chatThreadPager = chatClient.GetChatThreadsAsync();
            string acsThreadSummary;
            List<string> acsThreads = new List<string>();

            acsThreadSummary = "ACS Thread Summary:\n";
            await foreach (ChatThreadItem thread in chatThreadPager)
            {
                acsThreadSummary += $"{thread.Id} Deleted On: {thread.DeletedOn}\n";
                acsThreads.Add(thread.Id);
            }

            if (acsThreads.Count == 0)
            {
                acsThreadSummary += "No ACS Threads found\n";
            }
            else
            {
                acsThreadSummary += $"Found {acsThreads.Count} ACS Threads:\n";
            }

            return (acsThreadSummary, acsThreads);
        }

        async private static Task<(string agentConversationSummary, List<ConversationTableEntity> agentConversations)> GetAgentConversationSummary(IConfiguration config, List<string> acsThreads)
        {
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);
            CloudTable conversationTable = await storageHelper.GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);
            TableContinuationToken continuationToken = null;
            List<ConversationTableEntity> agentConversations = new List<ConversationTableEntity>();

            // Get all agent conversations 
            string agentConversationSummary = "\nAgent conversation summary:\n";
            do
            {
                var queryResult = await conversationTable.ExecuteQuerySegmentedAsync(new TableQuery<ConversationTableEntity>(), continuationToken);

                agentConversations.AddRange(queryResult.Results);

                foreach (var conversation in queryResult.Results)
                {
                    agentConversationSummary += $"{conversation.ThreadId}\n";
                }

                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            if (agentConversations.Count == 0)
            {
                agentConversationSummary += "No ACS Threads found\n";
            }
            else
            {
                agentConversationSummary += $"Found {agentConversations.Count} agent conversations\n";
            }

            // Check for orphands by removing matches from both Lists and if anything is left over in either, we have orphands
            ConversationTableEntity[] agentConversationsCopy = new ConversationTableEntity[agentConversations.Count];
            string[] acsThreadsCopy = new string[acsThreads.Count];

            // Have to make copies since you can't remove things from collections you are iterating over
            agentConversations.CopyTo(agentConversationsCopy);
            acsThreads.CopyTo(acsThreadsCopy);

            foreach (var conversation in agentConversationsCopy)
            {
                if (acsThreads.Contains(conversation.ThreadId))
                {
                    int c1 = acsThreads.Count;
                    acsThreads.Remove(conversation.ThreadId);
                    int c2 = acsThreads.Count;

                    int c3 = agentConversations.Count;
                    agentConversations.Remove(conversation);
                    int c4 = agentConversations.Count;
                }
            }

            return (agentConversationSummary, agentConversations);
        }

        private static string GetOrphandAgentConverationSummary(List<ConversationTableEntity> agentConversations)
        {
            ///
            /// Report on status of orphand agent conversations
            /// 

            string orphandAgentConverationSummary = "\nOrphand agent conversations (i.e., conversation record in the Storage Table with no matching ACS Thread):\n";
            foreach (var orphandConversation in agentConversations)
            {
                orphandAgentConverationSummary += $"{orphandConversation.ThreadId}\n";
            }

            if (agentConversations.Count == 0)
            {
                orphandAgentConverationSummary += "No orphand agent conversations found - All agent conversations had corresponding ACS threads\n";
            }
            else
            {
                orphandAgentConverationSummary += $"Found {agentConversations.Count} orphand agent conversations\n";
            }

            return orphandAgentConverationSummary;
        }

        private static string GetOrphandACSThreadSummary(List<string> acsThreads)
        {
            string orphandACSThreadSummary = "\nOrphand ACS Threads (i.e. ACS Threads with no matching conversation record in the Storage table)\n";
            foreach (var orphandACSThread in acsThreads)
            {
                orphandACSThreadSummary += $"{orphandACSThread}\n";
            }

            if (acsThreads.Count == 0)
            {
                orphandACSThreadSummary += "No orphand ACS Threads found - All ACS Threads had corresponding agent conversations\n";
            }
            else
            {
                orphandACSThreadSummary += $"Found {acsThreads.Count} orphand ACS Threads\n";
            }

            return orphandACSThreadSummary;
        }

        async private static Task<string> GetAgentSummary(IConfiguration config)
        {
            string agentSummary = "\nAgent Summary:\n";
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

            List<Agent> agents = await storageHelper.GetAgents(config);

            if (agents.Count > 0)
            {

                foreach (Agent agent in agents)
                {
                    string agentString = $"Agent Id: {agent.id}, Name: {agent.name}, Status: {agent.status}, Skills: {JsonConvert.SerializeObject(agent.skills)}\n";

                    agentSummary += agentString;
                }
            }
            else
            {
                agentSummary += "No agents found";
            }

            return agentSummary;
        }
    }
}
