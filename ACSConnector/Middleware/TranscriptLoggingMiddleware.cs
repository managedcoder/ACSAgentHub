using ACSConnector.Models;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACSConnector.Middleware
{
    public class TranscriptLoggingMiddleware : Microsoft.Bot.Builder.IMiddleware
    {
        private BotState _conversationState;

        public TranscriptLoggingMiddleware(ConversationState conversationState)
        {
            _conversationState = conversationState;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            var conversationStateAccessors = _conversationState.CreateProperty<LoggingConversationData>(nameof(LoggingConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new LoggingConversationData()).ConfigureAwait(false);

            // Log the transcript of the conversation so it can be passed to the agent upon escalation
            conversationData.ConversationLog.Add(turnContext.Activity);
            await _conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);

            turnContext.OnSendActivities(async (sendTurnContext, activities, nextSend) =>
            {
                conversationData.ConversationLog.AddRange(activities);
                await _conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
                // run full pipeline
                var responses = await nextSend().ConfigureAwait(false);
                return responses;
            });

            await next(cancellationToken).ConfigureAwait(false);
        }

    }
}
