using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACSConnector.Models
{
    public interface IACSAdapter
    {
        /// <summary>
        /// Provides interface for processing ACS events (agent messages go directly to ContinueConversationAsync())
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="botAppId"></param>
        /// <param name="conversationRef"></param>
        /// <param name="callback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ProcessActivityAsync(Activity activity, string botAppId, ConversationReference conversationRef, BotCallbackHandler callback, CancellationToken cancellationToken);
    }
}
