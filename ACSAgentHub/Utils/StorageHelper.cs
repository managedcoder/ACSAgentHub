using ACSAgentHubSDK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ACSAgentHub.Utils
{
    public class StorageHelper
    {
        string _connectionString;
        CloudStorageAccount _storageAccount;
        CloudTableClient _tableClient;

        public StorageHelper(string connectionString)
        {
            _connectionString = connectionString;
            _storageAccount = CloudStorageAccount.Parse(connectionString);
            _tableClient = _storageAccount.CreateCloudTableClient();
        }

        #region General Methods
        public async Task<CloudTable> GetTable(string tableName)
        {
            CloudTable table = _tableClient.GetTableReference(tableName);

            if (await table.CreateIfNotExistsAsync())
            {
                Console.WriteLine("Created Table named: {0}", tableName);
            }
            else
            {
                Console.WriteLine("Table {0} already exists", tableName);
            }

            Console.WriteLine();
            return table;
        }

        #endregion

        #region Service User Methods
        public async Task<ACSAgentUser> SaveAgentUser(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            CloudTable serviceUserTable = await GetTable(ACSAgentUser.AGENT_USER_TABLE_NAME);

            // Save the new service user to table storage
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(new ACSAgentUser(id));

            // Execute the operation.
            TableResult result = await serviceUserTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result as ACSAgentUser;
        }

        public async Task<ACSBotUser> SaveBotUser(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            CloudTable botUserTable = await GetTable(ACSBotUser.BOT_USER_TABLE_NAME);

            // Save the new service user to table storage
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(new ACSBotUser(id));

            // Execute the operation.
            TableResult result = await botUserTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result as ACSBotUser;
        }

        public async Task<ACSEndUser> SaveEndUser(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            CloudTable endUserTable = await GetTable(ACSEndUser.END_USER_TABLE_NAME);

            // Save the new service user to table storage
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(new ACSEndUser(id));

            // Execute the operation.
            TableResult result = await endUserTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result as ACSEndUser;
        }

        public async Task<string> GetAgentUserId()
        {
            CloudTable serviceUserTable = await GetTable(ACSAgentUser.AGENT_USER_TABLE_NAME);

            TableOperation retrieveOperation = TableOperation.Retrieve<ACSAgentUser>(ACSAgentUser.AGENT_USER_PARTITION_KEY, ACSAgentUser.AGENT_USER_ROW_KEY);
            TableResult result = await serviceUserTable.ExecuteAsync(retrieveOperation);
            ACSAgentUser serviceUser = result.Result as ACSAgentUser;

            return serviceUser?.ACS_Id;
        }

        public async Task<string> GetBotUserId()
        {
            CloudTable botUserTable = await GetTable(ACSBotUser.BOT_USER_TABLE_NAME);

            TableOperation retrieveOperation = TableOperation.Retrieve<ACSBotUser>(ACSBotUser.BOT_USER_PARTITION_KEY, ACSBotUser.BOT_USER_ROW_KEY);
            TableResult result = await botUserTable.ExecuteAsync(retrieveOperation);
            ACSBotUser botUser = result.Result as ACSBotUser;

            return botUser?.ACS_Id;
        }

        public async Task<string> GetEndUserId()
        {
            CloudTable endUserTable = await GetTable(ACSEndUser.END_USER_TABLE_NAME);

            TableOperation retrieveOperation = TableOperation.Retrieve<ACSEndUser>(ACSEndUser.END_USER_PARTITION_KEY, ACSEndUser.END_USER_ROW_KEY);
            TableResult result = await endUserTable.ExecuteAsync(retrieveOperation);
            ACSEndUser botUser = result.Result as ACSEndUser;

            return botUser?.ACS_Id;
        }


        #endregion

        #region Conversation Methods
        public async Task<ConversationTableEntity> AddToConversations(Conversation conversation)
        {
            CloudTable conversationTable = await GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);

            // Add the conversation to the table storage
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(new ConversationTableEntity(conversation));

            // Execute the operation.
            TableResult result = await conversationTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result as ConversationTableEntity;
        }

        async public Task<List<Conversation>> GetConversations()
        {
            CloudTable conversationTable = await GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);
            TableContinuationToken continuationToken = null;
            List<ConversationTableEntity> conversationTableEntities = new List<ConversationTableEntity>();
            List<Conversation> conversations = new List<Conversation>();

            // Get all agent conversations 
            do
            {
                var queryResult = await conversationTable.ExecuteQuerySegmentedAsync(new TableQuery<ConversationTableEntity>(), continuationToken);

                conversationTableEntities.AddRange(queryResult.Results);

                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            // Convert from table entity to object
            foreach (ConversationTableEntity conversationTableEntity in conversationTableEntities)
            {
                conversations.Add(new Conversation(conversationTableEntity));
            }

            return conversations.OrderByDescending(o => o.TimeStamp).ToList();
        }


        public async Task<ConversationTableEntity> GetConversation(string acsThreadId)
        {
            CloudTable conversationTable = await GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);

            TableOperation retrieveOperation = TableOperation.Retrieve<ConversationTableEntity>(ConversationTableEntity.CONVERSATION_PARTITION_KEY, acsThreadId);
            TableResult result = await conversationTable.ExecuteAsync(retrieveOperation);
            ConversationTableEntity conversation = result.Result as ConversationTableEntity;

            return conversation;
        }

        public async Task<ConversationTableEntity> DeleteConversationRecord(string acsThreadId)
        {
            CloudTable conversationTable = await GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);
            ConversationTableEntity conversation = await GetConversation(acsThreadId);

            if (conversation != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(conversation);
                TableResult result = await conversationTable.ExecuteAsync(deleteOperation);
            }

            return conversation;
        }

        #endregion

        #region Agents Methods

        async public Task<List<Agent>> GetAgents(IConfiguration config)
        {
            CloudTable agentTable = await GetTable(AgentTableEntity.AGENT_TABLE_NAME);
            TableContinuationToken continuationToken = null;
            List<AgentTableEntity> agentsTableEntities = new List<AgentTableEntity>();
            List<Agent> agents = new List<Agent>();

            // Get all agent conversations 
            do
            {
                var queryResult = await agentTable.ExecuteQuerySegmentedAsync(new TableQuery<AgentTableEntity>(), continuationToken);

                agentsTableEntities.AddRange(queryResult.Results);

                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            // Convert from table entity to object
            foreach(AgentTableEntity agentTableEntity in agentsTableEntities)
            {
                agents.Add(new Agent(agentTableEntity));
            }

            return agents;
        }

        async public Task<AgentTableEntity> AddToAgents(IConfiguration config, Agent agent)
        {
            CloudTable agentTable = await GetTable(AgentTableEntity.AGENT_TABLE_NAME);

            // Save the new service user to table storage
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(new AgentTableEntity(agent));

            // Execute the operation.
            TableResult result = await agentTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result as AgentTableEntity;
        }

        async public Task<Agent> GetAgent(IConfiguration config, string agentId)
        {
            CloudTable agentTable = await GetTable(AgentTableEntity.AGENT_TABLE_NAME);

            TableOperation tableOperation = TableOperation.Retrieve<AgentTableEntity>(AgentTableEntity.AGENT_PARTITION_KEY, agentId);
            TableResult tableResult = await agentTable.ExecuteAsync(tableOperation);

            return new Agent((AgentTableEntity) tableResult.Result);
        }

        async public Task<IActionResult> UpdateAgent(IConfiguration config, Agent agent)
        {
            CloudTable agentTable = await GetTable(AgentTableEntity.AGENT_TABLE_NAME);

            TableOperation tableOperation = TableOperation.Retrieve<AgentTableEntity>(AgentTableEntity.AGENT_PARTITION_KEY, agent.id);
            TableResult tableResult = await agentTable.ExecuteAsync(tableOperation);

            // Use implicit operator to update the retrieved agent table entity
            AgentTableEntity agentTableEntity = (AgentTableEntity)tableResult.Result;
            agentTableEntity.Update(agent);

            tableOperation = TableOperation.Replace(agentTableEntity);
            tableResult = await agentTable.ExecuteAsync(tableOperation);

            return new ContentResult() { StatusCode = tableResult.HttpStatusCode };
        }

        public async Task<ConversationTableEntity> UpdateConversation(string acsThreadId, ConversationStatus status)
        {
            // ToDo: Add error handling in this method
            CloudTable conversationTable = await GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);

            TableOperation tableOperation = TableOperation.Retrieve<ConversationTableEntity>(ConversationTableEntity.CONVERSATION_PARTITION_KEY, acsThreadId);
            TableResult tableResult = await conversationTable.ExecuteAsync(tableOperation);
            ConversationTableEntity conversationEntity = null;

            if (tableResult.HttpStatusCode >= 200 && tableResult.HttpStatusCode <= 299)
            {
                conversationEntity = tableResult.Result as ConversationTableEntity;

                conversationEntity.Status = (int)status;

                tableOperation = TableOperation.Replace(conversationEntity);
                tableResult = await conversationTable.ExecuteAsync(tableOperation);
            }

            return conversationEntity;
        }

        /// <summary>
        /// Update the conversation identified by conversation.ThreadId using the values of conversation
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public async Task<IActionResult> UpdateConversation(Conversation conversation)
        {
            // ToDo: Add error handling in this method
            CloudTable conversationTable = await GetTable(ConversationTableEntity.CONVERSATION_TABLE_NAME);

            TableOperation tableOperation = TableOperation.Retrieve<ConversationTableEntity>(ConversationTableEntity.CONVERSATION_PARTITION_KEY, conversation.ThreadId);
            TableResult tableResult = await conversationTable.ExecuteAsync(tableOperation);
            ConversationTableEntity conversationEntity = tableResult.Result as ConversationTableEntity;

            conversationEntity.Update(conversation);

            tableOperation = TableOperation.Replace(conversationEntity);
            tableResult = await conversationTable.ExecuteAsync(tableOperation);

            return new ContentResult() { StatusCode = tableResult.HttpStatusCode };
        }

        #endregion

    }
}
