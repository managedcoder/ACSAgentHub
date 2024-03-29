﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ACSConnector;
using ACSConnector.Middleware;
using ACSConnector.Models;
using VATemplateExample.Dialogs;
using VATemplateExample.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VATemplateExample.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter, IACSAdapter
    {
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly LocaleTemplateManager _templateEngine;
        private readonly SkillHttpClient _skillClient;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly BotSettings _settings;

        public DefaultAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            IChannelProvider channelProvider,
            AuthenticationConfiguration authConfig,
            LocaleTemplateManager templateEngine,
            ConversationState conversationState,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient,
            ILogger<BotFrameworkHttpAdapter> logger,
            TranscriptLoggingMiddleware loggingMiddleware,
            HandoffMiddleware handoffMiddleware,
            SkillsConfiguration skillsConfig = null,
            SkillHttpClient skillClient = null)
            : base(credentialProvider, authConfig, channelProvider, logger: logger)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateEngine = templateEngine ?? throw new ArgumentNullException(nameof(templateEngine));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _skillClient = skillClient;
            _skillsConfig = skillsConfig;
            _settings = settings;

            OnTurnError = HandleTurnErrorAsync;

            Use(telemetryMiddleware);
            Use(loggingMiddleware);
            Use(handoffMiddleware);

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new Microsoft.Bot.Solutions.Middleware.SetSpeakMiddleware());
        }

        private async Task HandleTurnErrorAsync(ITurnContext turnContext, Exception exception)
        {
            // Log any leaked exception from the application.
            _logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

            await SendErrorMessageAsync(turnContext, exception);
            await EndSkillConversationAsync(turnContext);
            await ClearConversationStateAsync(turnContext);
        }

        private async Task SendErrorMessageAsync(ITurnContext turnContext, Exception exception)
        {
            try
            {
                _telemetryClient.TrackException(exception);

                // Send a message to the user.
                await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("ErrorMessage"));

                // Send a trace activity, which will be displayed in the Bot Framework Emulator.
                // Note: we return the entire exception in the value property to help the developer;
                // this should not be done in production.
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught in SendErrorMessageAsync : {ex}");
            }
        }

        private async Task EndSkillConversationAsync(ITurnContext turnContext)
        {
            if (_skillClient == null || _skillsConfig == null)
            {
                return;
            }

            try
            {
                // Inform the active skill that the conversation is ended so that it has a chance to clean up.
                // Note: the root bot manages the ActiveSkillPropertyName, which has a value while the root bot
                // has an active conversation with a skill.
                var activeSkill = await _conversationState.CreateProperty<BotFrameworkSkill>(MainDialog.ActiveSkillPropertyName).GetAsync(turnContext, () => null);
                if (activeSkill != null)
                {
                    var endOfConversation = Activity.CreateEndOfConversationActivity();
                    endOfConversation.Code = "RootSkillError";
                    endOfConversation.ApplyConversationReference(turnContext.Activity.GetConversationReference(), true);

                    await _conversationState.SaveChangesAsync(turnContext, true);
                    await _skillClient.PostActivityAsync(_settings.MicrosoftAppId, activeSkill, _skillsConfig.SkillHostEndpoint, (Activity)endOfConversation, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught on attempting to send EndOfConversation : {ex}");
            }
        }

        private async Task ClearConversationStateAsync(ITurnContext turnContext)
        {
            try
            {
                // Delete the conversationState for the current conversation to prevent the
                // bot from getting stuck in a error-loop caused by being in a bad state.
                // ConversationState should be thought of as similar to "cookie-state" for a Web page.
                await _conversationState.DeleteAsync(turnContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught on attempting to Delete ConversationState : {ex}");
            }
        }

        /// <summary>
        /// Processes ACS events that come in from ACS Agent Hub
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="msAppId"></param>
        /// <param name="conversationRef"></param>
        /// <param name="callback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// The method just processes agent hub EVENTS.  Agent MESSAGES go directly from bot controller to ContinueConversationAsync()
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