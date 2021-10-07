using ACSConnector.Middleware;
using ACSConnector.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        public ACSAdapter(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, HandoffMiddleware handoffMiddleware, TranscriptLoggingMiddleware loggingMiddleware)
            : base(configuration, logger)
        {
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
                (ITurnContext proactiveContext, CancellationToken ct) =>
                {
                    using (var contextWithActivity = new TurnContext(this, activity))
                    {
                        contextWithActivity.TurnState.Add(proactiveContext.TurnState.Get<IConnectorClient>());
                        return base.RunPipelineAsync(contextWithActivity, callback, cancellationToken);
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

    }
}
