using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSConnector.Models
{
    public class LoggingConversationData
    {
        public List<Activity> ConversationLog { get; set; } = new List<Activity>();
    }
}
