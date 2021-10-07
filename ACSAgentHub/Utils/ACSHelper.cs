using Azure;
using Azure.Communication;
using Azure.Communication.Chat;
using Azure.Communication.Identity;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using ACSAgentHubSDK.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Text;
using Azure.Messaging.WebPubSub;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ACSAgentHub.Utils
{
    /// <summary>
    /// Summary description for ACSHelper
    /// </summary>
    public class ACSHelper
    {
        public ACSHelper()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// Creates an ACS User to represent the end user in ACS conversations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note: Creating an ACS user from an id is not as secure as creating an ACS user from a managed identity.
        /// Anyone with ACS access key, endpoint and user id can get access to the conversation threads.
        /// 
        /// If the ACS user has not been created yet or it was deleted from Azure Storage, a new ACS user will be created and
        /// the Id for that generated user will be saved to Azure Storage.  Think of this ACS user as a "service" user which
        /// we'll use in all ACS chat operations where the end user is speaking rather than have a separate user for each 
        /// end user. The actual name associated with the end user is provided separately which allows us to use this one user
        /// account for all end users and assign the name later when we use it.
        /// 
        /// To fully participate in a thread the application only needs to know:
        /// - Endpoint of ACS Service
        /// - ACS access key (needed to create an access token)
        /// - User Id of a valid and previously created ACS User
        /// - Access token (generated using endpoint, access key, and user Id) 
        /// 
        /// </remarks>
        internal static async Task<(CommunicationUserIdentifier user, string token)> GetACSEndUserAccessToken(IConfiguration config)
        {
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

            // Get agent user, if one exists
            string endUserId = await storageHelper.GetEndUserId();

            // Creates agent user from saved agentUserId or creates new service user if agentUserId is null
            var userAndAccessToken = await ACSHelper.GetUserAndAccessToken(config["acsConnectionString"], endUserId);

            // If agentUserId has not been saved yet (i.e. GetUserAndAccessToken just created it) then save it
            if (endUserId == null)
            {
                // Save the newly created agent user Id
                await storageHelper.SaveAgentUser(userAndAccessToken.user.Id);
            }

            return userAndAccessToken;
        }

        #region Identity and access methods

        /// <summary>
        /// Creates an ACS User to represent the agent in ACS conversations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note: Creating an ACS user from an id is not as secure as creating an ACS user from a managed identity.
        /// Anyone with ACS access key, endpoint and user id can get access to the conversation threads.
        /// 
        /// If the ACS user has not been created yet or it was deleted from Azure Storage, a new ACS user will be created and
        /// the Id for that generated user will be saved to Azure Storage.  Think of this ACS user as a "service" user which
        /// we'll use in all ACS chat operations where the agent is speaking rather than have a separate user for each agent. The
        /// actual name associated with the agent is provided separately which allows us to use this one user account for all
        /// agents and assign the name later when we use it.
        /// 
        /// To fully participate in a thread the application only needs to know:
        /// - Endpoint of ACS Service
        /// - ACS access key (needed to create an access token)
        /// - User Id of a valid and previously created ACS User
        /// - Access token (generated using endpoint, access key, and user Id) 
        /// 
        /// </remarks>
        public static async Task<(CommunicationUserIdentifier user, string token)> GetACSAgentUserAndAccessToken(IConfiguration config)
        {
            if (config["useACSManagedIdentity"].ToLower() == "true")
            {
                return await ACSHelper.GetManagedIdentityUserAndAccessToken(config["acsConnectionString"]);
            }
            else
            {
                StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

                // Get agent user, if one exists
                string agentUserId = await storageHelper.GetAgentUserId();

                // Creates agent user from saved agentUserId or creates new service user if agentUserId is null
                var userAndAccessToken = await ACSHelper.GetUserAndAccessToken(config["acsConnectionString"], agentUserId);

                // If agentUserId has not been saved yet (i.e. GetUserAndAccessToken just created it) then save it
                if (agentUserId == null)
                {
                    // Save the newly created agent user Id
                    await storageHelper.SaveAgentUser(userAndAccessToken.user.Id);
                }

                return userAndAccessToken;
            }
        }

        /// <summary>
        /// Creates an ACS User to represent the bot in ACS conversations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note: Creating an ACS user from an id is not as secure as creating an ACS user from a managed identity.
        /// Anyone with ACS access key, endpoint and user id can get access to the conversation threads.
        /// 
        /// If the ACS user has not been created yet or it was deleted from Azure Storage, a new ACS user will be created and
        /// the Id for that generated user will be saved to Azure Storage.  Think of this ACS user as a "service" user which
        /// we'll use in all ACS chat operations where the bot is speaking.
        /// 
        /// To fully participate in a thread the application only needs to know:
        /// - Endpoint of ACS Service
        /// - ACS access key (needed to create an access token)
        /// - User Id of a valid and previously created ACS User
        /// - Access token (generated using endpoint, access key, and user Id) 
        /// 
        /// </remarks>
        public static async Task<(CommunicationUserIdentifier user, string token)> GetACSBotUserAndAccessToken(IConfiguration config)
        {
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

            // Get bot user, if one exists
            string botUserId = await storageHelper.GetBotUserId();

            // Creates bot user from saved botUserId or creates new bot user if botUserId is null
            var userAndAccessToken = await ACSHelper.GetUserAndAccessToken(config["acsConnectionString"], botUserId);

            // If botUserId has not been saved yet (i.e. GetUserAndAccessToken just created it) then save it
            if (botUserId == null)
            {
                // Save the newly created bot user Id
                await storageHelper.SaveBotUser(userAndAccessToken.user.Id);
            }

            return userAndAccessToken;
        }

        /// <summary>
        /// Creates an ACS User to represent the end user in ACS conversations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note: Creating an ACS user from an id is not as secure as creating an ACS user from a managed identity.
        /// Anyone with ACS access key, endpoint and user id can get access to the conversation threads.
        /// 
        /// If the ACS user has not been created yet or it was deleted from Azure Storage, a new ACS user will be created and
        /// the Id for that generated user will be saved to Azure Storage.  Think of this ACS user as a "service" user which
        /// we'll use in all ACS chat operations where the end user is speaking rather than have a separate user for each end user.
        /// The actual name associated with the end user is provided separately which allows us to use this one user account for all
        /// end users and assign their name later when we use it.
        /// 
        /// To fully participate in a thread the application only needs to know:
        /// - Endpoint of ACS Service
        /// - ACS access key (needed to create an access token)
        /// - User Id of a valid and previously created ACS User
        /// - Access token (generated using endpoint, access key, and user Id) 
        /// 
        /// </remarks>
        public static async Task<(CommunicationUserIdentifier user, string token)> GetACSEndUserAndAccessToken(IConfiguration config)
        {
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);

            // Get end user, if one exists
            string endUserId = await storageHelper.GetEndUserId();

            // Creates end user from saved endUserId or creates new end user if endUserId is null
            var userAndAccessToken = await ACSHelper.GetUserAndAccessToken(config["acsConnectionString"], endUserId);

            // If endUserId has not been saved yet (i.e. GetUserAndAccessToken just created it) then save it
            if (endUserId == null)
            {
                // Save the newly created end user Id
                await storageHelper.SaveEndUser(userAndAccessToken.user.Id);
            }

            return userAndAccessToken;
        }


        /// <summary>
        /// Get the ACS user that the service created
        /// </summary>
        /// <param name="acsConnectionString">The ACS connection string from Azure portal</param>
        /// <param name="serviceUserId">The ACS user id.  If id is null, a new ACS user will be created and returned</param>
        /// <returns>Returns the ACS user and a ACS user access token</returns>
        public static async Task<(CommunicationUserIdentifier user, string token)> GetUserAndAccessToken(string acsConnectionString, string serviceUserId)
        {
            CommunicationIdentityClient identityClient = new CommunicationIdentityClient(new Uri(ACSHelper.ExtractEndpoint(acsConnectionString)), new AzureKeyCredential(ACSHelper.ExtractAccessKey(acsConnectionString)));
            CommunicationUserIdentifier serviceUser;
            Azure.Core.AccessToken tokenResponse;

            // If service user does not exist, then create one
            if (serviceUserId == null)
            {
                // Create service user
                serviceUser = identityClient.CreateUser();

                Console.WriteLine($"\nCreated new service user: {serviceUser.Id}");
            }
            else
            {
                Console.WriteLine($"\nRestored service user: {serviceUserId}");

                // Create service user based on id passed in
                serviceUser = new CommunicationUserIdentifier(serviceUserId);
            }

            // Get user access token with the "voip" scope for an identity
            tokenResponse = await identityClient.GetTokenAsync(serviceUser, scopes: new[] { CommunicationTokenScope.Chat });

            return (serviceUser, tokenResponse.Token);
        }

        /// <summary>
        /// Creates an ACS user from the Managed Identity capability of ACS
        /// </summary>
        /// <param name="endpoint">The ACS endpoint</param>
        /// <param name="accessKey">The ACS access key</param>
        /// <returns>Returns the ACS user and a ACS user access token</returns>
        /// <remarks>
        /// Uses Managed Identity to create the ACS user.  This method assumes Managed Identity has been properly 
        /// configured and the service configuration setting "useACSManagedIdentity" was set to true.  Think of this
        /// ACS user as a "service" user which will own all agent chat threads and we use this user in all ACS 
        /// operations rather than have separate ACS users for each agent and bot user.
        /// 
        /// Note: Creating a service user from from a managed identity is more secure than creating a service user 
        /// from an id.  Anyone with ACS access key, endpoint and user id can get access to the conversation threads.
        /// </remarks>
        public static async Task<(CommunicationUserIdentifier user, string token)> GetManagedIdentityUserAndAccessToken(string acsConnectionString)
        {
            CommunicationIdentityClient identityClient;
            CommunicationUserIdentifier managedIdentityUser;
            Azure.Core.AccessToken tokenResponse;

            // Retrieving new Access Token, using Managed Identities
            DefaultAzureCredential credential = new DefaultAzureCredential();

            // Create identity client using managed identity
            identityClient = new CommunicationIdentityClient(new Uri(ACSHelper.ExtractEndpoint(acsConnectionString)), new AzureKeyCredential(ACSHelper.ExtractAccessKey(acsConnectionString)));
            managedIdentityUser = (await identityClient.CreateUserAsync()).Value;

            // Issue an access token with the "voip" scope for an identity
            tokenResponse = await identityClient.GetTokenAsync(managedIdentityUser, scopes: new[] { CommunicationTokenScope.Chat });

            var userAccessToken = tokenResponse.Token;
            var expiresOn = tokenResponse.ExpiresOn;
            Console.WriteLine($"\nIssued the following access token with 'chat' scope that expires at {expiresOn}:");
            Console.WriteLine(userAccessToken);

            return (managedIdentityUser, userAccessToken);
        }

        async public static Task AddAgentUserToThread(IConfiguration config, string threadId, string displayName)
        {
            // The bot user owns the chat thread since it created it at start of escalation so we'll need it to add new participants
            var botUserAndAccessToken = await ACSHelper.GetACSBotUserAndAccessToken(config);
            // Get the "service account" for all agent users
            var agentUserAndAccessToken = await ACSHelper.GetACSAgentUserAndAccessToken(config);

            // Create a chat participant for the agent and here is where we provide the name of the agent
            var agentUserChatParticipant = new ChatParticipant(agentUserAndAccessToken.user)
            {
                DisplayName = displayName
            };

            // Create a chat client from the user that owns/created the thread
            ChatClient botUserChatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new CommunicationTokenCredential(botUserAndAccessToken.token));
            ChatThreadClient botUserChatThreadClient = botUserChatClient.GetChatThreadClient(threadId: threadId);

            // Users can only be added by another user who is already in the thread so we'll use the bot user to add the agent
            // Only user who've been added to the thread can send messages to it so we need to add the agent to the thread
            // Only adds user to thread if they are not yet on the thread, otherwise no action is taken
            botUserChatThreadClient.AddParticipant(agentUserChatParticipant);
        }

        async public static Task AddEndUserToThread(IConfiguration config, string threadId, string displayName)
        {
            // Get bot user and bot user's access token
            var botUserAndAccessToken = await ACSHelper.GetACSBotUserAndAccessToken(config);
            // Get end user and end user's access token
            var endUserAndAccessToken = await ACSHelper.GetACSEndUserAndAccessToken(config);

            // Create a chat participant for the end user and set its name
            var endUserChatParticipant = new ChatParticipant(endUserAndAccessToken.user)
            {
                DisplayName = displayName
            };

            // Create a chat client for the bot user
            ChatClient botUserChatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new CommunicationTokenCredential(botUserAndAccessToken.token));
            ChatThreadClient botUserChatThreadClient = botUserChatClient.GetChatThreadClient(threadId: threadId);

            // Users can only be added by another user who is already in the thread so we'll use the bot user to add the agent
            // Only user who've been added to the thread can send messages to it so we need to add the agent to the thread
            // Only adds user to thread if they are not yet on the thread, otherwise no action is taken
            botUserChatThreadClient.AddParticipant(endUserChatParticipant);
        }

        #endregion

        #region Thread CRUD methods

        async public static Task<ChatThreadClient> CreateEscalationThread(IConfiguration config, EscalationContext context)
        {
            var botUserAndToken = await ACSHelper.GetACSBotUserAndAccessToken(config);
            var endUserAndToken = await ACSHelper.GetACSEndUserAndAccessToken(config);

            ChatClient botChatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new CommunicationTokenCredential(botUserAndToken.token));
            ChatClient endUserChatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new CommunicationTokenCredential(endUserAndToken.token));

            var botUser = new ChatParticipant(botUserAndToken.user)
            {
                // We'll represent the virtual assist as "bot" but it might be nice to all the name of the assistant to somehow be supplied
                DisplayName = "bot"
            };

            var endUser = new ChatParticipant(endUserAndToken.user)
            {
                DisplayName = context.handoffContext.Name
            };

            CreateChatThreadResult createBotUserChatThreadResult = await botChatClient.CreateChatThreadAsync(topic: $"Conversation with {endUser.DisplayName}", participants: new[] { botUser, endUser });
            string sharedThreadId = createBotUserChatThreadResult.ChatThread.Id;

            ChatThreadClient botUserChatThreadClient = botChatClient.GetChatThreadClient(threadId: sharedThreadId);
            ChatThreadClient endUserChatThreadClient = endUserChatClient.GetChatThreadClient(threadId: sharedThreadId);

            await WriteTranscriptToACSChatThread(context, botUser, endUser, botUserChatThreadClient, endUserChatThreadClient);

            Console.WriteLine($"Created escalation thread for botUser \"{context.handoffContext.Name }\" using bot service user: {botUserAndToken.user.Id} ");

            return botUserChatThreadClient;
        }

        private static async Task WriteTranscriptToACSChatThread(EscalationContext context, ChatParticipant botUser, ChatParticipant endUser, ChatThreadClient botUserChatThreadClient, ChatThreadClient endUserChatThreadClient)
        {
            if (context.transcript != null)
            {
                foreach (var transcriptItem in context.transcript)
                {
                    if (transcriptItem.UserName.ToLower() == "bot")
                    {
                        await botUserChatThreadClient.SendMessageAsync(content: transcriptItem.Message, type: ChatMessageType.Text, botUser.DisplayName);
                    }
                    else
                    {
                        // Must be user
                        await endUserChatThreadClient.SendMessageAsync(content: transcriptItem.Message, type: ChatMessageType.Text, endUser.DisplayName);
                    }
                }
            }            
        }


        private static string ExtractTranscriptAsString(List<MinimizedTranscriptItem> transcript)
        {
            StringBuilder sb = new StringBuilder();

            foreach (MinimizedTranscriptItem item in transcript)
            {
                sb.AppendFormat("{0}: {1}", item.UserName, item.Message);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        async public static Task DeleteEscalationThread(IConfiguration config, string acsThreadId)
        {
            try
            {
                var userAndToken = await ACSHelper.GetACSBotUserAndAccessToken(config);
                ChatClient chatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new CommunicationTokenCredential(userAndToken.token));
            
                await chatClient.DeleteChatThreadAsync(acsThreadId);
            }
            catch (Exception)
            {
                // Ignore any error that happens on delete since there can be a race condition with 2 agents attempting
                // to delete a thread that was closed by an end user at the same time
            }
        }


        #endregion

        #region Message CRUD methods

        public static async Task SendMessageToBot(IConfiguration config, ACSAgentHubSDK.Models.ChatMessage chatMessage)
        {

            // Use thread Id to get the conversation
            StorageHelper storageHelper = new StorageHelper(config["agentHubStorageConnectionString"]);
            ConversationTableEntity conversationTableEntity = await storageHelper.GetConversation(chatMessage.threadId);

            if (conversationTableEntity != null)
            {
                // Create an agent hub message
                AgentHubMessage agentHubMessage = new AgentHubMessage()
                {
                    Conversation = new Conversation(conversationTableEntity),
                    ChatMessage = chatMessage
                };

                string botBaseAddress = config["botBaseAddress"].EndsWith('/') ? config["botBaseAddress"] : config["botBaseAddress"] + "/";
                var postRequest = new HttpRequestMessage(HttpMethod.Post, $"{botBaseAddress}api/ACSConnector/MessageToBot/{conversationTableEntity.ThreadId}")
                {
                    Content = JsonContent.Create(agentHubMessage),
                    // ToDo: Set headers here when we add security to bot controller APIs
                };

                // Send agent message to bot
                var postResponse = await Startup.httpClient.SendAsync(postRequest);

                postResponse.EnsureSuccessStatusCode();
            }
            else
            {
                // The bot has no record of this conversation, this should not happen
                throw new Exception("Cannot find conversation");
            }
        }

        /// <summary>
        /// Send a message to an agent
        /// </summary>
        /// <param name="config"></param>
        /// <param name="acsThreadId"></param>
        /// <param name="botUser"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sending a message to an agent involves using ACS to send a message to a chat thread.  You might be wondering why
        /// there is no ACSHelper to SendMessageToBot() and the reason is that bot users listen on bot conversations, not ACS
        /// chat threads (hence it's not a ACSHelp related activity, thus no corresponding method).  Sending messages to bot
        /// users involve a REST call to the bot's controller and is handled in postMessageToBot Azure Function.
        /// </remarks>
        async public static Task SendMessageToAgent(IConfiguration config, string acsThreadId, string message)
        {
            try
            {
                var userAndToken = await ACSHelper.GetACSBotUserAndAccessToken(config);
                ChatClient chatClient = new ChatClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new CommunicationTokenCredential(userAndToken.token));

                CommunicationIdentityClient identityClient = new CommunicationIdentityClient(new Uri(ACSHelper.ExtractEndpoint(config["acsConnectionString"])), new AzureKeyCredential(ACSHelper.ExtractAccessKey(config["acsConnectionString"])));
                string threadId = acsThreadId;
                string id = userAndToken.user.Id;
                string token = userAndToken.token;
                string endpoint = ACSHelper.ExtractEndpoint(config["acsConnectionString"]);
                CommunicationUserIdentifier serviceUser = new CommunicationUserIdentifier(id);
                Azure.Core.AccessToken tokenResponse = await identityClient.GetTokenAsync(serviceUser, scopes: new[] { CommunicationTokenScope.Chat });
                // return (serviceUser, tokenResponse.Token);

                var chatParticipant = new ChatParticipant(userAndToken.user)
                {
                    DisplayName = "from ACSHelper.SM"
                };

                //CreateChatThreadResult createChatThreadResult = await chatClient.CreateChatThreadAsync(topic: $"Conversation with {chatParticipant.DisplayName}", participants: new[] { chatParticipant });
                ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId: acsThreadId);

                chatThreadClient.AddParticipant(chatParticipant);

                // Drop transcript/context in thread so Agent will have it when conversation starts later
                var messageId = await chatThreadClient.SendMessageAsync(message, ChatMessageType.Text);
            }
            catch (Exception e)
            {
                string emessage = e.Message;

                throw e;
            }
        }

        #endregion

        #region Web PubSub methods

        async public static Task PublishRefreshEvent(IConfiguration config, string threadId=null)
        {
            // Create a Web PubSub client so that we can notify agent-hub clients to refresh conversations
            var serviceClient = new WebPubSubServiceClient(config["webPusSubConnectionString"], config["webPubSubHubName"]);

            // Broadcast "refresh event" (note: currently, agent-hub clients don't use the string "refresh"
            // but its a required argument so we have to pass something.  Agent-hubs subscribe to
            // _config["hub"] messages and merely receiving a message is the signal to refresh conversations
            dynamic obj = new JObject();
            obj.eventType = "refresh";
            obj.threadId = threadId;
            string refreshEvent = JsonConvert.SerializeObject(obj);

            await serviceClient.SendToAllAsync(refreshEvent);

            Console.WriteLine($"Published a Web PubSub 'Refresh' event {refreshEvent} at: {DateTime.Now}");
        }

        #endregion

        #region UtilityMethods

        static public string ExtractAccessKey(string acsConnectionString)
        {
            return acsConnectionString.Substring(acsConnectionString.ToLower().IndexOf("accesskey=") + 10);
        }

        static public string ExtractEndpoint(string connectionString)
        {
            return connectionString.Replace("endpoint=", "").Split(';')[0];
        }

        #endregion
    }
}
