using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ACSConnector.Models
{
    /// I'm not going to use ConversationMap approach that the LivePerson implementation did and
    /// instead I'll store conversation records in persistant storage so they can be shared across
    /// web app instances in case solution is scaled out across multiple web app instances

    //public class ConversationMap
    //{
    //    public ConcurrentDictionary<string, ConversationRecord> ConversationRecords { get; set; } = new ConcurrentDictionary<string, ConversationRecord>();
    //}
}
