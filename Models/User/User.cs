using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Trnkt.Models
{
    [DynamoDBTable("Users")]
    public class User
    {    
        [DynamoDBHashKey]
        public string Id { get; set; }

        [DynamoDBProperty]
        public string UserName { get; set; }

        [DynamoDBProperty]
        public string Email { get; set; }

        [DynamoDBProperty]
        public string PasswordHash { get; set; }

        [DynamoDBProperty]
        public List<FavoritesList> Favorites { get; set; }
    }
}