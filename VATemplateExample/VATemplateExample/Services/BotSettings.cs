// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using VATemplateExample.TokenExchange;
using Microsoft.Bot.Solutions;

namespace VATemplateExample.Services
{
    public class BotSettings : BotSettingsBase
    {
        public TokenExchangeConfig TokenExchangeConfig { get; set; }
    }
}