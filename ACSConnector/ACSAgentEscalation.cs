using ACSConnector.Middleware;
using ACSConnector.Models;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ACSConnector
{
    public class ACSAgentEscalation : BotComponent
    {
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // This does not work yet
            var url = configuration.GetSection("acsAgentHubBaseAddress").Value;

            // Register agent hub dependencies
            services.AddSingleton<ACSConnector>();
            services.AddSingleton<IMiddleware, HandoffMiddleware>();
            services.AddSingleton<IMiddleware, TranscriptLoggingMiddleware>();

            services.AddHttpClient<AgentHubHttpClient>(client =>
            {
                client.BaseAddress = new Uri(url);
                // ToDo: Add headers to access Agent Hub APIs
                // client.DefaultRequestHeaders.Add("???", "???");
            });
        }
    }
}
