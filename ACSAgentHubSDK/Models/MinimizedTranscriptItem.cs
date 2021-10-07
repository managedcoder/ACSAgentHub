using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    /// A minimal set of properties to represent a transcript item
    /// </summary>
    /// <remarks>
    /// The Bot Framework's Transcript class is a IList<Activity> and the Activity class is large with only
    /// a few properties that are needed for a transcript so this class allows us to slim a transcript down
    /// to a List<MinimizedTranscriptItem>
    /// </remarks>
    public class MinimizedTranscriptItem
    {
        public string UserName { get; set; }
        public string Message { get; set; }
    }
}
