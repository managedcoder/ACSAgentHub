using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    /// The ACS User that represents all bot users
    /// </summary>
    /// <remarks>
    /// ACSBotUser is like a server-type of user so there is only one ACSBotUser that is shared 
    /// across all BotUser instances.
    /// </remarks>
    public class ACSBotUser : TableEntity
    {
        public static string BOT_USER_TABLE_NAME = "BotUser";
        public static string BOT_USER_PARTITION_KEY = "BotUserPartition";
        public static string BOT_USER_ROW_KEY = "BotUserRow";

        // Need parameterless constructor for Table SDK deserialization to work when retrieving entities from table
        public ACSBotUser() { }

        public ACSBotUser(string acsId)
        {
            this.PartitionKey = BOT_USER_PARTITION_KEY;
            this.RowKey = BOT_USER_ROW_KEY;
            ACS_Id = acsId;
        }

        public string ACS_Id { get; set; }
    }

}
