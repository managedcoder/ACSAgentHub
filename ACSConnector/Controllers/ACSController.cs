using ACSAgentHubSDK.Models;
using ACSConnector.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACSConnector.Controllers
{
    [Route("api/ACSConnector")]
    [ApiController]
    public class ACSController : ControllerBase
    {
        private readonly BotAdapter _adapter;
        private readonly IBot _bot;
        private string _botAppId;
        // This is the Id of the ACS service user that the bot service will use when accessing ACS services
        private static string _acsBotUserId;
        // This is the Id of the ACS service user for the agent user which we'll will use when accessing ACS services
        private static string _acsAgentUserId;
        AgentHubHttpClient _diInjectedHttpClient;
        private readonly ConversationState _conversationState;

        public ACSController(IConfiguration configuration,
                             BotAdapter adapter,
                             IBot bot,
                             ConversationState conversationState,
                             AgentHubHttpClient httpClient)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _adapter = adapter;
            _bot = bot;
            _botAppId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
            _diInjectedHttpClient = httpClient;
        }

        /// <summary>
        /// Incoming Agent Hub event
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("EventToBot/{acsThreadId}")]
        public async Task PostEventAsync(string acsThreadId)
        {
            System.Diagnostics.Debug.WriteLine("In api/EventToBot!!!!");

            using (var readStream = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string body = await readStream.ReadToEndAsync();

                try
                {
                    Conversation conversation = JsonConvert.DeserializeObject<Conversation>(body);

                    if (conversation == null)
                    {
                        // Must set StatusCode before you write to response
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("Missing Conversation object in body of POST"));

                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("Saw AgentHubEvent");

                    // Create an agent hub "change of status" event
                    var evnt = EventFactory.CreateHandoffStatus(conversation.EscalationContext.conversationReference.Conversation, conversation.Status.ToString().ToLower()) as Activity;
                    // Inject event into the bot's processing pipeline
                    await ProcessActivityAsync(_adapter, evnt, _botAppId, conversation.EscalationContext.conversationReference, _bot.OnTurnAsync, default(CancellationToken));

                    // Must set StatusCode before you write to response
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"Event processed successfully"));

                    return;
                }
                catch (Exception e)
                {
                    // Must set StatusCode before you write to response
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"Invalid AgentHubEvent object in body of POST: {e.Message}"));

                    return;
                }
            }
        }

        /// <summary>
        /// Incoming Agent Hub message
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("MessageToBot/{acsThreadId}")]
        public async Task PostMessageAsync(string acsThreadId)
        {
            System.Diagnostics.Debug.WriteLine("In api/MessageToBot!!!!");

            using (var readStream = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string requestBody = await readStream.ReadToEndAsync();

                try
                {
                    AgentHubMessage agentHubMessage = JsonConvert.DeserializeObject<AgentHubMessage>(requestBody);

                    if (agentHubMessage != null)
                    {
                        Activity agentMessage = MessageFactory.Text(agentHubMessage.ChatMessage.messageBody);
                        
                        // Only fetch _acsBotUserId once and only if we haven't fetched yet
                        if (_acsBotUserId == null)
                        {
                            var response = await _diInjectedHttpClient.Client.GetAsync($"api/botAccessContext");
                            response.EnsureSuccessStatusCode();

                            dynamic botAccessContext = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                            _acsBotUserId = botAccessContext.userId;
                        }

                        // Only fetch _acsAgentUserId once and only if we haven't fetched yet
                        if (_acsAgentUserId == null)
                        {
                            var response = await _diInjectedHttpClient.Client.GetAsync($"api/agentAccessContext");
                            response.EnsureSuccessStatusCode();

                            dynamic agentAccessContext = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                            _acsAgentUserId = agentAccessContext.userId;
                        }

                        // Note: The integration with ACS Chat threads involves an Event Grid that uses a webhook to 
                        // forward any message the is written to the chat thread.  For the agent, this is exactly what
                        // we want and its how messages get from agent to bot.  However, this also happens when the bot
                        // post messages to the chat thread which creates an echo where the message the bot send boomerangs
                        // back to the bot.  This conditional only allows agent messages to flow on to bot if its an
                        // active conversation AND the message is NOT echo message.
                        if (agentHubMessage.Conversation.Status != ConversationStatus.Closed &&
                            agentHubMessage.ChatMessage.senderCommunicationIdentifier.communicationUser.id == _acsAgentUserId)
                        {
                            await _adapter.ContinueConversationAsync(
                                _botAppId,
                                agentHubMessage.Conversation.EscalationContext.conversationReference,
                                (ITurnContext turnContext, CancellationToken cancellationToken) =>
                                    turnContext.SendActivityAsync(agentMessage, cancellationToken),
                                default(CancellationToken));
                        }

                        Response.StatusCode = (int)HttpStatusCode.OK;
                        await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"Message processed successfully"));
                    }
                    else
                    {
                        // Must set StatusCode before you write to response
                        Response.StatusCode = (int)HttpStatusCode.BadRequest; 
                        await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"Message object missing from body of {Request.Method} to {Request.Path.Value}"));
                    }
                }
                catch (Exception e)
                {
                    // Must set StatusCode before you write to response
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"Invalid message object in body of POST: {e.Message}"));

                    return;
                }
            }
        }

        private async Task ProcessActivityAsync(BotAdapter adapter, Activity activity, string msAppId, ConversationReference conversationRef, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            BotAssert.ActivityNotNull(activity);

            activity.ApplyConversationReference(conversationRef, true);

            await adapter.ContinueConversationAsync(
                msAppId,
                conversationRef,
                async (ITurnContext proactiveContext, CancellationToken ct) =>
                {
                    using (var contextWithActivity = new TurnContext(adapter, activity))
                    {
                        contextWithActivity.TurnState.Add(proactiveContext.TurnState.Get<IConnectorClient>());
                        await callback(proactiveContext, cancellationToken);

                        if (contextWithActivity.Activity.Name == "handoff.status")
                        {
                            Activity replyActivity;
                            var state = (contextWithActivity.Activity.Value as JObject)?.Value<string>("state");
                            if (state == "typing")
                            {
                                replyActivity = new Activity
                                {
                                    Type = ActivityTypes.Typing,
                                    Text = "agent is typing",
                                };
                            }
                            else if (state == "accepted")
                            {
                                replyActivity = MessageFactory.Text("An agent has accepted the conversation and will respond shortly.");
                                await _conversationState.SaveChangesAsync(contextWithActivity);
                            }
                            else if (state == "closed")
                            {
                                replyActivity = MessageFactory.Text("The agent has ended the conversation and you're now reconnected with the digital assistant");

                                // Route the conversation based on whether it's been escalated
                                var conversationStateAccessors = _conversationState.CreateProperty<EscalationRecord>(nameof(EscalationRecord));
                                var escalationRecord = await conversationStateAccessors.GetAsync(contextWithActivity, () => new EscalationRecord()).ConfigureAwait(false);

                                // End the escalation
                                escalationRecord.EndEscalation();
                                await _conversationState.SaveChangesAsync(contextWithActivity).ConfigureAwait(false);
                            }
                            else
                            {
                                replyActivity = MessageFactory.Text($"Conversation status changed to '{state}'");
                            }

                            await contextWithActivity.SendActivityAsync(replyActivity);
                        }

                    }
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
