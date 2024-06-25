using System;
using System.ComponentModel;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Trnkt.Models
{
    [DynamoDBTable("Favorites")]
    public class UserFavorites
    {
        // Partition Key
        [DynamoDBHashKey] 
        public string UserId { get; set; }

        // Map to DynamoDB attribute
        [DynamoDBProperty("Favorites")] 
        public List<FavoritesList> Favorites { get; set; }
    }
}
