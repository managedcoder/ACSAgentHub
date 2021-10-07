using ACSAgentHubSDK.Models;
using ACSConnector.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACSConnector.Middleware
{
    public class HandoffMiddleware : IMiddleware
    {
        private AgentHubHttpClient _agentHttpClient;
        private readonly BotState _conversationState;

        public HandoffMiddleware(ConversationState conversationState, AgentHubHttpClient agentHttpClient)
        {
            _conversationState = conversationState;
            _agentHttpClient = agentHttpClient;
        }

        async public Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            // Route the conversation based on whether it's been escalated
            var conversationStateAccessors = _conversationState.CreateProperty<EscalationRecord>(nameof(EscalationRecord));
            var escalationRecord = await conversationStateAccessors.GetAsync(turnContext, () => new EscalationRecord()).ConfigureAwait(false);

            if (UserWantsToEndCall(turnContext.Activity.Text))
            {
                await turnContext.SendActivityAsync("Your conversation with the agent has ended and you're now reconnected with the digital assistant");

                // End the conversation (note: this call could fail due to race condition of agent and bot user
                // ending call at same time, thus we'd be messaging a delete thread, but since we don't care
                // about return value it won't matter
                await ACSConnector.SendEventToAgentAsync(_agentHttpClient, new BotEvent() { ThreadId = escalationRecord.ThreadId, Status = ConversationStatus.Closed });

                // End the escalation
                escalationRecord.EndEscalation();
                await _conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);

                return;
            }

            // If this is a message and the conversation is escalated then broker message to human agent
            if (turnContext.Activity.Type == ActivityTypes.Message && escalationRecord.IsConversationEscalated)
            {
                await ACSConnector.BrokerMessageToAgentAsync(_agentHttpClient, escalationRecord.ThreadId, turnContext.Activity.Text, turnContext.Activity.From.Name);

                // In broker-mode we only want messages to go to human agent so we need to end pipeline process
                // by returning without calling next()
                return;
            }

            // If this is a Handoff event
            if (turnContext.Activity.Type == ActivityTypes.Event && turnContext.Activity.Name == HandoffEventNames.HandoffStatus)
            {
                try
                {
                    var state = (turnContext.Activity.Value as JObject)?.Value<string>("state");
                    if (state == "closed")
                    {
                        // End the escalation
                        escalationRecord.EndEscalation();
                        await _conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
                    }
                    else if (state == "accepted")
                    {
                        // Nothing for middleware to do for this event which will be handled when it bubbles to MainDialog
                    }
                }
                catch
                {
                    await turnContext.SendActivityAsync("The Context property of Send Handoff Activity was null or invalid JSON.  ");

                }
            }

            turnContext.OnSendActivities(async (sendTurnContext, activities, nextSend) =>
            {
                // Handle any escalation events, and let them propagate through the pipeline
                // This is useful for debugging with the Emulator
                var handoffEvents = activities.Where(activity =>
                    activity.Type == ActivityTypes.Event && activity.Name == HandoffEventNames.InitiateHandoff);

                if (handoffEvents.Count() == 1)
                {
                    Activity handoffEvent = handoffEvents.First();

                    escalationRecord.ThreadId = await ACSConnector.Escalate(_agentHttpClient, sendTurnContext, handoffEvent, turnContext.Activity.From.Name).ConfigureAwait(false);
                    await _conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
                }

                // run full pipeline
                var responses = await nextSend().ConfigureAwait(false);
                return responses;
            });

            await next(cancellationToken).ConfigureAwait(false);
        }

        private bool UserWantsToEndCall(string text)
        {
            string t = text?.ToLower();

            return t == "quit" || t == "cancel" || t == "hang up" || t == "end" || t == "stop" || 
                   t == "close" || t == "done" || t == "good bye" || t == "bye" || t == "bye bye";
        }
    }
}
