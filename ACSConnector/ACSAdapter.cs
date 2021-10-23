using ACSConnector.Middleware;
using ACSConnector.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACSConnector
{
    /// <summary>
    /// Provide a default Bot Framework Adaptor for bots that don't need a custom adaptor
    /// </summary>
    /// <remarks>
    /// Can be used in bots solutions that don't have their own implementation of an adaptor.  This class is not
    /// used in Virtual Assistant Template bots since that template comes with it's own implementation of an
    /// adaptor and therefore implements its own separate IACSAdapter
    /// 
    /// Bots that don't need a custom adaptor can use this class in ConfigureServices in the Startup.cs file 
    /// like this:
    ///      // Create the Bot Framework Adapter.
    //       services.AddSingleton<BotFrameworkHttpAdapter, ACSAdapter>();
    /// </remarks>
    public class ACSAdapter : BotFrameworkHttpAdapter, IACSAdapter
    {
        private readonly ConversationState _conversationState;

        public ACSAdapter(
            IConfiguration configuration, 
            ILogger<BotFrameworkHttpAdapter> logger, 
            HandoffMiddleware handoffMiddleware,
            ConversationState conversationState,
            TranscriptLoggingMiddleware loggingMiddleware)
            : base(configuration, logger)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            Use(handoffMiddleware);
            Use(loggingMiddleware);

            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                // Send a message to the user
                await turnContext.SendActivityAsync($"The bot encounted an error '{exception.Message}'.").ConfigureAwait(false);

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError").ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Implements processing of ACS events (agent messages go directly to ContinueConversationAsync())
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="msAppId"></param>
        /// <param name="conversationRef"></param>
        /// <param name="callback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// Can be used in bots solutions that don't have their own implementation of an adaptor.  This class is not
        /// used in Virtual Assistant Template bots since that template comes with it's own implementation of an
        /// adaptor and therefore implements its own separate IACSAdapter
        /// </remarks>
        public async Task ProcessActivityAsync(Activity activity, string msAppId, ConversationReference conversationRef, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            BotAssert.ActivityNotNull(activity);

            activity.ApplyConversationReference(conversationRef, true);

            await ContinueConversationAsync(
                msAppId,
                conversationRef,
                async (ITurnContext proactiveContext, CancellationToken ct) =>
                {
                    using (var contextWithActivity = new TurnContext(this, activity))
                    {
                        contextWithActivity.TurnState.Add(proactiveContext.TurnState.Get<IConnectorClient>());
                        await base.RunPipelineAsync(contextWithActivity, callback, cancellationToken);

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
