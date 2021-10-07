// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace VATemplateExample.TokenExchange
{
    public class TokenExchangeConfig : ITokenExchangeConfig
    {
        public string Provider { get; set; }

        public string ConnectionName { get; set; }
    }
}