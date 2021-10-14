using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ACSAgentHub.Utils;
using Microsoft.Extensions.Configuration;
using Azure.Communication;

namespace ACSAgentHub.APIs.Access
{
    public class getAgentAccessContext
    {
        IConfiguration _config;
        public getAgentAccessContext(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Get ACS access token for agents to chat on given threadId
        /// </summary>
        /// <param name="req"></param>
        /// <param name="context"></param>
        /// <param name="log"></param>
        /// <param name="userType"></param>
        /// <returns></returns>
        /// <remarks>
        /// There is only one ACS users for all agents that acts as a sort of service account.  The display
        /// name for a particular agent is set when agent is added to a thread and is local to that thread
        /// so all agents share the same acs user Id but each has a separate display name that is set per
        /// thread.
        /// </remarks>
        [FunctionName("getAgentAccessContext")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agentAccessContext")] HttpRequest req,
            ILogger log)
        {
            (CommunicationUserIdentifier user, string token) userAndToken;

            userAndToken = await ACSHelper.GetACSAgentUserAndAccessToken(_config);

            return new OkObjectResult(new { userId = userAndToken.user.Id, token = userAndToken.token,  endPoint = ACSHelper.ExtractEndpoint(_config["acsConnectionString"]) });
        }
    }
}
