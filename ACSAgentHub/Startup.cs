using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ACSAgentHub.Startup))]

namespace ACSAgentHub
{
    public class Startup : FunctionsStartup
    {
        // Static connect until we can get Depency Injection to work right for HttpClient in Configure()
        public static HttpClient httpClient = new HttpClient();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Azure Function makes HttpClientFactory available via dependency inject but it requires
            // the Microsoft.Extensions.Http library but currently NuGet will only allow version 5 due
            // to other dependencies but version 5 is not currently supported by Azure Functions
            // v3 so including it causes the service to fail at runtime in statup so I am abandoning
            // the proper Dependency Injection approach outlined in the follow 2 links until this
            // issue gets resolved:
            // https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection#register-services
            // https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection#use-injected-dependencies
            // For now, I will go with the guidance we gave before we had DI which can be
            // found here: https://docs.microsoft.com/en-us/azure/azure-functions/manage-connections
            //builder.Services.AddHttpClient();
        }
    }
}

