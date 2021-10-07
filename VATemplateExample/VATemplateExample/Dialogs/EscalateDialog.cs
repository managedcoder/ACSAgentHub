// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ACSAgentHubSDK.Models;
using ACSConnector.Models;
using VATemplateExample.Models;
using VATemplateExample.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VATemplateExample.Dialogs
{
    // Example onboarding dialog to initial user profile information.
    public class EscalateDialog : ComponentDialog
    {
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileAccessor;
        private readonly IStatePropertyAccessor<LoggingConversationData> _conversationStateAccessors;

        public EscalateDialog(
            IServiceProvider serviceProvider)
            : base(nameof(EscalateDialog))
        {
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();

            var userState = serviceProvider.GetService<UserState>();
            _userProfileAccessor = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            var conversationState = serviceProvider.GetService<ConversationState>();
            _conversationStateAccessors = conversationState.CreateProperty<LoggingConversationData>(nameof(LoggingConversationData));

            _services = serviceProvider.GetService<BotServices>();

            var onboarding = new WaterfallStep[]
            {
                AskForNameAsync,
                AskWhyTheyNeedHelpAsync,
                FinishEscalateDialogAsync,
            };

            AddDialog(new WaterfallDialog(nameof(onboarding), onboarding));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));
            AddDialog(new TextPrompt(DialogIds.WhyTheyNeedHelpPrompt));
        }

        public async Task<DialogTurnResult> AskForNameAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await _userProfileAccessor.GetAsync(sc.Context, () => new UserProfileState(), cancellationToken);

            // This message says, "Our agents are available 24/7 at 1(800)555-1234. Or connect with us through Microsoft Teams"
            // We'll want to change this to, "Ok, I going to connect you with an agent now, it might take a minute or so..."
            await sc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("EscalateMessage"), cancellationToken);

            if (!string.IsNullOrEmpty(state.Name))
            {
                return await sc.NextAsync(state.Name, cancellationToken);
            }

            return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
            {
                Prompt = _templateManager.GenerateActivityForLocale("NamePrompt"),
            }, cancellationToken);
        }

        public async Task<DialogTurnResult> AskWhyTheyNeedHelpAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileAccessor.GetAsync(sc.Context, () => new UserProfileState(), cancellationToken);
            var name = (string)sc.Result;

            var generalResult = sc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
            if (generalResult == null)
            {
                var localizedServices = _services.GetCognitiveModels();
                generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(sc.Context, cancellationToken);
                sc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
            }

            (var generalIntent, var generalScore) = generalResult.TopIntent();
            if (generalIntent == GeneralLuis.Intent.ExtractName && generalScore > 0.5)
            {
                if (generalResult.Entities.PersonName_Any != null)
                {
                    name = generalResult.Entities.PersonName_Any[0];
                }
                else if (generalResult.Entities.personName != null)
                {
                    name = generalResult.Entities.personName[0];
                }
            }

            // Capitalize name
            userProfile.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

            // Save user's name in their user profile which completes the responsibilities to deal with answer from last step
            await _userProfileAccessor.SetAsync(sc.Context, userProfile, cancellationToken);

            // Now let's turn our attention to what we need ask the user in this step
            return await sc.PromptAsync(DialogIds.WhyTheyNeedHelpPrompt, new PromptOptions()
            {
                Prompt = _templateManager.GenerateActivityForLocale("WhyTheyNeedHelpPrompt"),
            }, cancellationToken);
        }

        public async Task<DialogTurnResult> FinishEscalateDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileAccessor.GetAsync(sc.Context, () => new UserProfileState(), cancellationToken);
            var conversationData = await _conversationStateAccessors.GetAsync(sc.Context, () => new LoggingConversationData());
            var whyTheyNeedHelp = (string)sc.Result;

            var transcript = new Transcript(conversationData.ConversationLog.Where(a => a.Type == ActivityTypes.Message).ToList());

            var evnt = EventFactory.CreateHandoffInitiation(sc.Context,
                // This is the custom ACS handoff context... include what you want and grab it out in AgentHub's escalateToAgent logic
                new HandoffContext()
                {
                    Skill = "offshore accouts",
                    Name = userProfile.Name,
                    CustomerType = "vip",
                    WhyTheyNeedHelp = whyTheyNeedHelp
                },
                transcript);

            // ToDo: Use the following pattern to generate a locaized response AND SEATCH EVERY OCCURANCE OF SendActivityAsync() to make sure we have it everywhere
            // await sc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("YourMessageID", userProfile), cancellationToken);

            await sc.Context.SendActivityAsync("Ok, I'll connect you to an agent. One moment, please…");

            await sc.Context.SendActivityAsync(evnt);

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string NamePrompt = "NamePrompt";
            public const string WhyTheyNeedHelpPrompt = "WhyTheyNeedHelpPrompt";
        }
    }
}
