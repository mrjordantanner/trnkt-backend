using System;
using System.ComponentModel;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Trnkt.Models
{
    [DynamoDBTable("Users")]
    public class User
    {
        // Partition Key
        [DynamoDBHashKey] 
        public string Email { get; set; }

        [DynamoDBProperty]
        public string UserId { get; set; }

        [DynamoDBProperty]
        public string UserName { get; set; }

        [DynamoDBProperty]
        public string PasswordHash { get; set; }

        [DynamoDBProperty]
        public string CreatedAt { get; set; }
    }
}
