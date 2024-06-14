using System;
using System.ComponentModel;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Trnkt.Models
{
    [DynamoDBTable("Users")]
    public class User
    {
        [DynamoDBHashKey] // Specify Email as the Hash Key (Partition Key)
        public string Email { get; set; }

        [DynamoDBProperty]
        public string Id { get; set; }

        [DynamoDBProperty]
        public string UserName { get; set; }

        [DynamoDBProperty]
        public string PasswordHash { get; set; }

        // [DynamoDBProperty(typeof(DateTimeConverter))]
        // public DateTime CreatedAt { get; set; }

        // [DynamoDBProperty("Favorites")] // Example for mapping to DynamoDB attribute
        // public List<FavoritesList> Favorites { get; set; }
    }
}
