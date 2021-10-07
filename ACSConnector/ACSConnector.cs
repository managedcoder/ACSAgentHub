using ACSAgentHubSDK.Models;
using ACSConnector.Middleware;
using ACSConnector.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ACSConnector
{
    /// <summary>
    /// Provides life cycle integration (begin, broken, end) with ACS
    /// </summary>
    public class ACSConnector
    {
        static async public Task<string> Escalate(AgentHubHttpClient agentHubClient, ITurnContext turnContext, Activity handoffEvent, string username)
        {
            List<MinimizedTranscriptItem> minimizedTranscript = null;

            // Grab the custom data from the value property of the handoffEvent and get the user name and "why" from there since
            // the user name will have been properly rationalized in the escalation dialog where we first check to see if
            // turnContext.Activity.From.Name is set (this would be set by the browser or client based on autheicated user
            // name) and if not, then see if Name in User Profile was set and use it if it has already been set, otherwise
            // prompt in the Escalation Dialog for the user name and ask for "why" also.  Both these things will be part of
            // the custom Json object that was set to the handoffEvent.Value property

            /* hERE IS WHAT THAT SHAPE LOOKS LIKE
                new
                {
                    Skill = "offshore accouts",
                    Name = userProfile.Name,
                    CustomerType = "vip",
                    WhyTheyNeedHelp = whyTheyNeedHelp
                },
            */

            // The Value property is an object type so it can be passed anything so we cast to what we expect
            // from an ACSConnector implementation
            HandoffContext handoffContext = new HandoffContext(handoffEvent.Value);

            // If handoff event has a transcript
            if (handoffEvent.Attachments.Count > 0)
            {
                // If the escalation has a transcript, the EventFactory.CreateHandoffInitiation() method will put the transcript in the attachments so we have to dig it out from there...
                Attachment transcriptAttachment = handoffEvent.Attachments[0];
                // Transcript is a big object so lets strip it down to just what we want out of it
                minimizedTranscript = CreateMinimizedTranscript(transcriptAttachment);
            }

            if (!handoffContext.IsValid())
                throw new ArgumentException("The handoffContext.Value property that was pass to EventFactory.CreateHandoffInitiation was null or not a ACSAgentHubSDK.Models.HandoffContext object with all properties set to appropriate values"); 

            // turnContext has turnContext.Activity.GetConversationReference() which we will need to save to conversation in table
            // handoffEvent has the context we'll need to specify Skill and anything else we want the AgentHub to
            // have and you'll get it from  var context = handoffEvent.Value as JObject;... var skill = context?.Value<string>("Skill");

            // Call Agent Hub's escalateToAgent which will 1) create a new ACS Thread and 2) add a new conversation to the Storage Table
            // Note: ACSConnector can't access ACSUtils since it's in the AgentHub 
            EscalationContext escalationContext = new EscalationContext() { conversationReference = turnContext.Activity.GetConversationReference(), handoffContext = handoffContext, transcript = minimizedTranscript};

            var content = new StringContent(JsonConvert.SerializeObject(escalationContext), Encoding.UTF8, "application/json");

            // Send bot event to agent
            HttpResponseMessage escalationResult = await agentHubClient.Client.PostAsync("api/escalateToAgent", content);

            // Return the threadId of the conversation
            return await escalationResult.Content.ReadAsStringAsync();
        }

        private static List<MinimizedTranscriptItem> CreateMinimizedTranscript(Attachment attachment)
        {
            Transcript transcript = attachment.Content as Transcript;
            List<MinimizedTranscriptItem> result = new List<MinimizedTranscriptItem>();

            // If we don't have a transcript attached then return empty transcript
            if (transcript == null)
                return result;

            //Activity a = transcript.Activities[0];

            //DateTimeOffset? ts = a.Timestamp;
            //DateTimeOffset? tss = a.LocalTimestamp;

            //DateTimeOffset? ts2 = a.Timestamp;
            //DateTimeOffset? tss2 = a.LocalTimestamp;

            //bool b = ts < ts2;
            //bool bb = tss < tss2;

            //Console.WriteLine("Before:");
            //Console.WriteLine(string.Join(Environment.NewLine, transcript.Activities));

            //((List<Activity>)transcript.Activities).Sort(delegate (Activity c1, Activity c2) { return c1.Timestamp.Value.CompareTo(c2.Timestamp.Value); });

            //Console.WriteLine("After:");
            //Console.WriteLine(string.Join(Environment.NewLine, transcript.Activities));

            System.Diagnostics.Debug.WriteLine(string.Join(Environment.NewLine, transcript.Activities));

            foreach (Activity activity in transcript.Activities)
            {
                // Adaptive Card responses come as attachments as do some text sent using SendActicityAsync so deal with that here
                if (activity.Text == null)
                {
                    // If response is contained in an attachment, extract it if you can
                    if (activity.Attachments.Count > 0)
                    {
                        string message = null;

                        if (string.IsNullOrEmpty(activity.Speak))
                            message = "<omitted response - unsupported message type>";
                        else
                        {
                            XmlDocument speechXml = new XmlDocument();

                            speechXml.LoadXml(activity.Speak);
                            message = speechXml?.DocumentElement["voice"]?.InnerText;
                        }

                        result.Add(new MinimizedTranscriptItem() { UserName = activity.From.Name, Message =  message });
                    }
                }
                else
                {
                    result.Add(new MinimizedTranscriptItem() { UserName = activity.From.Name, Message = activity.Text });
                }
            }

            return result;
        }

        static async public Task<HttpResponseMessage> BrokerMessageToAgentAsync(AgentHubHttpClient agentHubClient, string threadId, string message, string username)
        {
            BotMessage botMessage = new BotMessage() { text = message, type = "text",  userName = username };

            var content = new StringContent(JsonConvert.SerializeObject(botMessage), Encoding.UTF8, "application/json");

            // Send bot event to agent
            return await agentHubClient.Client.PostAsync($"api/conversations/{threadId}/messageToAgent", content);
        }

        static async public Task<HttpResponseMessage> SendEventToAgentAsync(AgentHubHttpClient agentHubClient, BotEvent botEvent)
        {
            var content = new StringContent(JsonConvert.SerializeObject(botEvent), Encoding.UTF8, "application/json");

            // Send bot event to agent
            return await agentHubClient.Client.PostAsync($"api/conversations/{botEvent.ThreadId}/eventToAgent", content);
        }
    }
}
