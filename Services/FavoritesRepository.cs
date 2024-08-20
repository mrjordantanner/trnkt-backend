using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trnkt.Models;
using Trnkt.Configuration;
using Microsoft.Extensions.Options;


namespace Trnkt.Services
{
    public class FavoritesRepository : IFavoritesRepository
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly ILogger<FavoritesRepository> _logger;
        private readonly Dictionary<string, UserFavorites> _cache;
        private readonly AppConfig _settings;

        public FavoritesRepository(
            IAmazonDynamoDB dynamoDbClient,
            IOptions<AppConfig> settings,
            ILogger<FavoritesRepository> logger)
        {
            _dynamoDbClient = dynamoDbClient;
            _logger = logger;
            _settings = settings.Value;
            _cache = new Dictionary<string, UserFavorites>();
        }

        // Get UserFavorites for given UserId
        public async Task<UserFavorites> GetUserFavoritesAsync(string userId)
        {
            try
            {
                // Fetch the existing UserFavorites object
                var getItemRequest = new GetItemRequest
                {
                    TableName = _settings.FavoritesTableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "UserId", new AttributeValue { S = userId } }
                    }
                };

                var getItemResponse = await _dynamoDbClient.GetItemAsync(getItemRequest);

                if (getItemResponse.Item == null || getItemResponse.Item.Count == 0)
                {
                    _logger.LogWarning($"UserFavorites for User {userId} not found.");
                    return null;
                }

                return MapDynamoDbItemToUserFavorites(getItemResponse.Item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"UserFavorites for User {userId} not found: {ex}");
                return null;
            }
        }

        // Get UserFavorites for given UserId with additional logic
        // TODO may need refactoring
        public async Task<UserFavorites> GetFavoritesAsync(string userId)
        {
            // TODO refactor this logic for when to return cached values and when to hit DynamoDb
            // Return cached data if available
            // if (_cache.TryGetValue(userId, out var cachedFavorites))
            // {
            //     _logger.LogInformation("Found userId {userId} in local Cache - Returning cached Favorites.", userId);
            //     return cachedFavorites;
            // }

            var request = new GetItemRequest
            {
                TableName = _settings.FavoritesTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } }
                }
            };

            try
            {
                _logger.LogInformation("Getting UserFavorites for UserId {userId}...", userId);

                // Check if record for this UserId exists in DynamoDb yet
                var getItemResponse = await _dynamoDbClient.GetItemAsync(request);
                if (getItemResponse.Item == null || getItemResponse.Item.Count == 0)
                {
                    // If not, create an empty UserFavorites and return it 
                    var emptyFavorites = new UserFavorites
                    {
                        UserId = userId,
                        Favorites = new List<FavoritesList>()
                        {
                            new FavoritesList()
                            {
                                ListId = "Default-List",
                                Name = "Favorites 1",
                                Nfts = new List<Nft>()
                            }
                        }
                    };

                    _logger.LogInformation("No favorites for UserId {userId} found -- Returning empty (default) UserFavorites", userId);
                    return emptyFavorites;
                }

                // If a pre-existing UserFavorites record is found, map the DB response to a UserFavorites object
                // _logger.LogInformation("Found existing Favorites record for {userId}", userId);

                var favorites = MapDynamoDbItemToUserFavorites(getItemResponse.Item);
                _cache[userId] = favorites; // Update local cache
                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting Favorites for User {userId}", ex);
                throw;
            }
        }

        // UPDATE Favorites STEP 5
        // TODO refactor using DynamoDb update instead of replacing entire object?
        public async Task<UserFavorites> UpdateFavoritesAsync(string userId, FavoritesList[] updatedLists)
        {
            if (updatedLists == null)
            {
                _logger.LogError($"UpdateFavoritesAsync-- Update failed. Argument 'updatedLists' was null.");
                return null;
            }

            //foreach (var list in updatedLists)
            //{
            //    foreach (var nft in list.Nfts)
            //    {   
            //        Console.WriteLine($"NFT from params: {nft.Name}, ImgUrl: {nft.ImageUrl}");
            //    }
            //}

            bool isModified = false;
            var userFavorites = await GetFavoritesAsync(userId) ?? new UserFavorites { UserId = userId, Favorites = new List<FavoritesList>() };

            // Iterate through the UPDATED lists that have been provided
            foreach (var updatedList in updatedLists)
            {
                var existingList = userFavorites.Favorites.FirstOrDefault(list => list.ListId == updatedList.ListId);

                // If the ListId already exists, perform comparisons to check for changes
                if (existingList != null)
                {
                    bool nftsAreDifferent = existingList.Nfts.Count != updatedList.Nfts.Count ||
                                            !existingList.Nfts.All(nft => updatedList.Nfts.Any(uNft => uNft.Identifier == nft.Identifier));

                    // If changes are found, update the ExistingList's properties with the UpdatedList's properties
                    if (existingList.Name != updatedList.Name || nftsAreDifferent)
                    {

                        if (existingList.Nfts.Count > 0 && updatedList.Nfts.Count > 0) {
                        Console.WriteLine($"NFT: ExistingList: {existingList.Nfts[0].ImageUrl}, UpdatedList: {updatedList.Nfts[0].ImageUrl}");
                        }

                        existingList.Name = updatedList.Name;
                        existingList.Nfts = updatedList.Nfts;
                        isModified = true;
                        _logger.LogInformation($"Updated existing Favorites List. UserId: {userId}, ListId: {updatedList.ListId}, Name: {updatedList.Name}, NFT Count: {updatedList.Nfts.Count}");
                    }
                }
                // If the ListId did not previously exist, add it to Favorites as a new List
                else
                {
                    userFavorites.Favorites.Add(updatedList);
                    isModified = true;
                    _logger.LogInformation("Added new Favorites List {listName} for User {userId}", updatedList.Name, userId);
                }
            }

            // If changes were detected, write the new FavoritesLists data to DynamoDb
            if (isModified)
            {
                _logger.LogInformation($"UserFavorites were modified. Writing {userFavorites.Favorites.Count} FavoritesLists to DynamoDb...");

                var putItemRequest = CreatePutItemRequest(userFavorites);
                try
                {
                    await _dynamoDbClient.PutItemAsync(putItemRequest);
                    _cache[userId] = userFavorites; // Update the cache after successful write
                    _logger.LogInformation("Favorites updated successfully for User {userId}", userId);
                    return userFavorites;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating Favorites for User {userId}", ex);
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("No changes detected for User {userId}", userId);
                return userFavorites;
            }
        }

        // DELETE entire UserFavorites object - This only gets used when a User's account is deleted
        public async Task<bool> DeleteUserFavoritesAsync(string userId)
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "UserId", new AttributeValue { S = userId } }
            };

            var deleteItemRequest = new DeleteItemRequest
            {
                TableName = _settings.FavoritesTableName,
                Key = key
            };

            try
            {
                var response = await _dynamoDbClient.DeleteItemAsync(deleteItemRequest);
                return ((int)response.HttpStatusCode < 300);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting Favorites for User {userId}", ex);
                return false;
            }
        }

        // DELETE a single FavoritesList
        public async Task<bool> DeleteFavoritesListAsync(string userId, string listIdToDelete)
        {
            try
            {
                // Get current UserFavorites
                var userFavorites = await GetUserFavoritesAsync(userId);
                if (userFavorites == null)
                {
                    _logger.LogWarning($"UserFavorites for User {userId} not found.");
                    return false;
                }

                // Find the list to delete
                var listIndex = userFavorites.Favorites.FindIndex(list => list.ListId == listIdToDelete);
                if (listIndex == -1)
                {
                    _logger.LogWarning($"FavoritesList {listIdToDelete} for User {userId} not found.");
                    return false;
                }

                // Define the update expression to remove the specific FavoritesList
                var updateExpression = $"REMOVE #Favorites[{listIndex}]";

                // Create the request to update the UserFavorites object
                var updateItemRequest = new UpdateItemRequest
                {
                    TableName = _settings.FavoritesTableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "UserId", new AttributeValue { S = userId } }
                    },
                    UpdateExpression = updateExpression,
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#Favorites", "Favorites" }
                    },
                    ConditionExpression = "attribute_exists(Favorites)" // Ensure the Favorites attribute exists before trying to delete
                };

                // Execute the update request
                var updateItemResponse = await _dynamoDbClient.UpdateItemAsync(updateItemRequest);

                if (updateItemResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Failed to delete FavoritesList {listIdToDelete} for User {userId}. HTTP Status Code: {updateItemResponse.HttpStatusCode}");
                    return false;
                }

                _logger.LogInformation($"FavoritesList {listIdToDelete} deleted for User {userId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting FavoritesList {listIdToDelete} for User {userId}: {ex}");
                return false;
            }
        }


        // DELETE individual NFT from a FavoritesList
        public async Task<bool> DeleteNftFromFavoritesListAsync(string userId, string listId, string nftIdToDelete)
        {
            try
            {
                // Get current UserFavorites
                var userFavorites = await GetUserFavoritesAsync(userId);
                if (userFavorites == null)
                {
                    _logger.LogWarning($"UserFavorites for User {userId} not found.");
                    return false;
                }

                // Locate the specific FavoritesList by listId
                var favoritesList = userFavorites.Favorites.FirstOrDefault(fl => fl.ListId == listId);

                if (favoritesList == null)
                {
                    _logger.LogWarning($"FavoritesList {listId} not found for User {userId}.");
                    return false;
                }

                // Check if the specified NFT exists in the FavoritesList
                var nftExists = favoritesList.Nfts.Any(nft => nft.Identifier == nftIdToDelete);

                if (!nftExists)
                {
                    _logger.LogWarning($"Nft {nftIdToDelete} not found in FavoritesList {listId} for User {userId}.");
                    return false;
                }

                // Update the UserFavorites object in DynamoDB using the new UpdateItemRequest
                var updateItemRequest = CreateUpdateItemRequest(userFavorites, listId, nftIdToDelete);
                var response = await _dynamoDbClient.UpdateItemAsync(updateItemRequest);

                if ((int)response.HttpStatusCode < 300)
                {
                    _logger.LogInformation($"Nft {nftIdToDelete} removed from FavoritesList {listId} for User {userId}.");
                    return true;
                }

                _logger.LogError("FavoritesRepository: DeleteNftFromFavoritesList failed with status code {code}", response.HttpStatusCode);
                return false;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing Nft {nftIdToDelete} from FavoritesList {listId} for User {userId}: {ex.Message}", ex);
                return false;
            }
        }


        // HELPERS
        // Mappers and DynamoDB Helper Methods
        private static UserFavorites MapDynamoDbItemToUserFavorites(Dictionary<string, AttributeValue> item)
        {
            return new UserFavorites
            {
                UserId = item["UserId"].S,
                Favorites = item["Favorites"].L.Select(fl => new FavoritesList
                {
                    ListId = fl.M["ListId"].S,
                    Name = fl.M["Name"].S,
                    Nfts = fl.M["Nfts"].L.Select(nft => new Nft
                    {
                        Identifier = nft.M["Identifier"].S,
                        Collection = nft.M["Collection"].S,
                        Contract = nft.M["Contract"].S,
                        Name = nft.M["Name"].S,
                        ImageUrl = nft.M["ImageUrl"].S,
                        AnimationUrl = nft.M["AnimationUrl"].S,
                        OpenseaUrl = nft.M["OpenseaUrl"].S
                    }).ToList()
                }).ToList()
            };
        }

        // If needed, remove cache entry for the given userId so the next read will read from DynamoDb
        // public void InvalidateCache(string userId)
        // {
        //     _cache.Remove(userId);
        // }

        private PutItemRequest CreatePutItemRequest(UserFavorites userFavorites)
        {
            return new PutItemRequest
            {
                TableName = _settings.FavoritesTableName,
                Item = new Dictionary<string, AttributeValue>
                    {
                        { "UserId", new AttributeValue { S = userFavorites.UserId } },
                        { "Favorites", new AttributeValue
                            {
                                L = userFavorites.Favorites?
                                    .Where(list => list != null &&
                                                !string.IsNullOrEmpty(list.ListId) &&
                                                !string.IsNullOrEmpty(list.Name) &&
                                                list.Nfts != null &&
                                                list.Nfts.Count > 0)
                                    .Select(list =>
                                    {
                                        //_logger.LogInformation("Processing list: " + list.ListId);
                                        return new AttributeValue
                                        {
                                            M = new Dictionary<string, AttributeValue>
                                            {
                                                { "ListId", new AttributeValue { S = list.ListId } },
                                                { "Name", new AttributeValue { S = list.Name } },
                                                { "Nfts", new AttributeValue
                                                    {
                                                        L = list.Nfts?
                                                            .Where(nft => nft != null &&
                                                                        !string.IsNullOrEmpty(nft.Identifier))
                                                            .Select(nft =>
                                                            {
                                                                return new AttributeValue
                                                                {
                                                                    M = new Dictionary<string, AttributeValue>
                                                                    {
                                                                        { "Identifier", new AttributeValue { S = nft.Identifier ?? string.Empty } },
                                                                        { "Collection", new AttributeValue { S = nft.Collection ?? string.Empty } },
                                                                        { "Contract", new AttributeValue { S = nft.Contract ?? string.Empty } },
                                                                        { "Name", new AttributeValue { S = nft.Name ?? string.Empty } },
                                                                        { "ImageUrl", new AttributeValue { S = nft.ImageUrl ?? string.Empty } },
                                                                        { "AnimationUrl", new AttributeValue { S = nft.AnimationUrl ?? string.Empty } },
                                                                        { "OpenseaUrl", new AttributeValue { S = nft.OpenseaUrl ?? string.Empty } },
                                                                    }
                                                                };
                                                            }).ToList() ?? new List<AttributeValue>()
                                                    }
                                                }
                                            }
                                        };
                                    }).ToList() ?? new List<AttributeValue>()
                            }
                        }
                    }
            };
        }

        // Create an UpdateItemRequest to remove an individual NFT from a FavoritesList
        private UpdateItemRequest CreateUpdateItemRequest(UserFavorites userFavorites, string listId, string nftIdentifierToRemove)
        {
            _logger.LogInformation("Creating UpdateItemRequest...");

            var updateExpressions = new List<string>();
            var expressionAttributeNames = new Dictionary<string, string>();

            var favoritesListIndex = userFavorites.Favorites.FindIndex(list => list.ListId == listId);
            if (favoritesListIndex == -1)
            {
                _logger.LogError($"FavoritesList with ListId {listId} not found.");
                return null;
            }

            var nftIndexToRemove = userFavorites.Favorites[favoritesListIndex].Nfts.FindIndex(nft => nft.Identifier == nftIdentifierToRemove);
            if (nftIndexToRemove == -1)
            {
                _logger.LogError($"NFT with Identifier {nftIdentifierToRemove} not found in FavoritesList {listId}.");
                return null;
            }

            // Build the update expression for removing the NFT
            var updateExpression = $"REMOVE #Favorites[{favoritesListIndex}].#Nfts[{nftIndexToRemove}]";

            expressionAttributeNames["#Favorites"] = "Favorites";
            expressionAttributeNames["#Nfts"] = "Nfts";

            _logger.LogWarning($"UpdateExpression: {updateExpression}");

            return new UpdateItemRequest
            {
                TableName = _settings.FavoritesTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userFavorites.UserId } }
                },
                UpdateExpression = updateExpression,
                ExpressionAttributeNames = expressionAttributeNames
            };
        }

        // Logging Helper Methods
        private string SerializeAttributeValue(AttributeValue value)
        {
            if (value.S != null) return value.S;
            if (value.N != null) return value.N;
            if (value.B != null) return Convert.ToBase64String(value.B.ToArray());
            if (value.SS != null) return string.Join(", ", value.SS);
            if (value.NS != null) return string.Join(", ", value.NS);
            if (value.BS != null) return string.Join(", ", value.BS.Select(b => Convert.ToBase64String(b.ToArray())));
            if (value.M != null) return "{" + string.Join(", ", value.M.Select(kv => kv.Key + ": " + SerializeAttributeValue(kv.Value))) + "}";
            if (value.L != null) return "[" + string.Join(", ", value.L.Select(SerializeAttributeValue)) + "]";
            if (value.NULL) return "NULL";
            if (value.BOOL != null) return value.BOOL.ToString();

            return "Unknown AttributeValue Type";
        }

        private static void LogAttributeValue(string key, AttributeValue value, string indent = "")
        {
            if (value.S != null)
            {
                Console.WriteLine($"{indent}{key}: {value.S}");
            }
            else if (value.N != null)
            {
                Console.WriteLine($"{indent}{key}: {value.N}");
            }
            else if (value.B != null)
            {
                Console.WriteLine($"{indent}{key}: {value.B}");
            }
            else if (value.SS != null)
            {
                Console.WriteLine($"{indent}{key}: [{string.Join(", ", value.SS)}]");
            }
            else if (value.NS != null)
            {
                Console.WriteLine($"{indent}{key}: [{string.Join(", ", value.NS)}]");
            }
            else if (value.BS != null)
            {
                Console.WriteLine($"{indent}{key}: [{string.Join(", ", value.BS)}]");
            }
            else if (value.M != null)
            {
                Console.WriteLine($"{indent}{key}:");
                foreach (var mapItem in value.M)
                {
                    LogAttributeValue(mapItem.Key, mapItem.Value, indent + "  ");
                }
            }
            else if (value.L != null)
            {
                Console.WriteLine($"{indent}{key}: [");
                foreach (var listItem in value.L)
                {
                    LogAttributeValue(key, listItem, indent + "  ");
                }
                Console.WriteLine($"{indent}]");
            }
            else if (value.NULL)
            {
                Console.WriteLine($"{indent}{key}: NULL");
            }
            else if (value.BOOL != null)
            {
                Console.WriteLine($"{indent}{key}: {value.BOOL}");
            }
        }
    }
}
