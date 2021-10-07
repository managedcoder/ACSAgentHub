using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACSAgentHubSDK.Models
{
    /// <summary>
    /// The ACS User that represents all end users
    /// </summary>
    /// <remarks>
    /// ACSEndUser is like a server-type of user so there is only one ACSEndUser that is shared 
    /// across all EndUser instances. The name of the end user will be assigned later when 
    /// escalation happens.
    /// </remarks>
    public class ACSEndUser : TableEntity
    {
        public static string END_USER_TABLE_NAME = "EndUser";
        public static string END_USER_PARTITION_KEY = "EndUserPartition";
        public static string END_USER_ROW_KEY = "EndUserRow";

        // Need parameterless constructor for Table SDK deserialization to work when retrieving entities from table
        public ACSEndUser() { }

        public ACSEndUser(string acsId)
        {
            this.PartitionKey = END_USER_PARTITION_KEY;
            this.RowKey = END_USER_ROW_KEY;
            ACS_Id = acsId;
        }

        public string ACS_Id { get; set; }
    }

}
