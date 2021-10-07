using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using ACSAgentHub.Utils;
using Azure.Communication;

namespace ACSAgentHub.APIs.Access
{
    public class getEndUserAccessContext
    {
        IConfiguration _config;
        public getEndUserAccessContext(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// This is a test method at this point since it won't be called from the agent-portal
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("getEndUserAccessContext")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "EndUserAccessContext")] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            (CommunicationUserIdentifier user, string token) userAndToken;

            userAndToken = await ACSHelper.GetACSEndUserAccessToken(_config);

            return new OkObjectResult(new { userId = userAndToken.user.Id, token = userAndToken.token, endPoint = ACSHelper.ExtractEndpoint(_config["acsConnectionString"]) });
        }
    }
}
