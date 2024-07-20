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

        // Update Favorites STEP 5
        public async Task<UserFavorites> UpdateFavoritesAsync(string userId, FavoritesList[] updatedLists)
        {
            if (updatedLists == null)
            {
                _logger.LogError($"UpdateFavoritesAsync-- Update failed. Argument 'updatedLists' was null.");
                return null;
            }

            foreach (var list in updatedLists)
            {
                foreach (var nft in list.Nfts)
                {   
                    Console.WriteLine($"NFT from params: {nft.Name}, ImgUrl: {nft.ImageUrl}");
                }
            }

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

        // DELETE entire UserFavorites object
        public async Task DeleteUserFavoritesAsync(string userId)
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
                await _dynamoDbClient.DeleteItemAsync(deleteItemRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting Favorites for User {userId}", ex);
            }
        }
        
        // DELETE an individual FavoritesList from a UserFavorites
        public async Task DeleteFavoritesListAsync(string userId, string listIdToDelete)
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
                if (!getItemResponse.Item.Any())
                {
                    _logger.LogError($"UserFavorites for User {userId} not found.");
                    return;
                }

                var userFavorites = MapDynamoDbItemToUserFavorites(getItemResponse.Item);

                // Remove the specified FavoritesList
                var updatedFavoritesLists = userFavorites.Favorites
                    .Where(list => list.ListId != listIdToDelete)
                    .ToList();

                if (updatedFavoritesLists.Count == userFavorites.Favorites.Count)
                {
                    _logger.LogError($"FavoritesList {listIdToDelete} not found for User {userId}.");
                    return;
                }

                userFavorites.Favorites = updatedFavoritesLists;

                // Update the UserFavorites object in DynamoDB
                var updateItemRequest = CreateUpdateItemRequest(userFavorites);
                await _dynamoDbClient.UpdateItemAsync(updateItemRequest);

                _logger.LogInformation($"FavoritesList {listIdToDelete} deleted for User {userId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting FavoritesList {listIdToDelete} for User {userId}", ex);
            }
        }

        public async Task DeleteNftFromFavoritesListAsync(string userId, string listId, string nftIdToDelete)
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

                if (!getItemResponse.Item.Any())
                {
                    _logger.LogWarning($"UserFavorites for User {userId} not found.");
                    return;
                }

                var userFavorites = MapDynamoDbItemToUserFavorites(getItemResponse.Item);

                // Locate the specific FavoritesList by listId
                var favoritesList = userFavorites.Favorites.FirstOrDefault(fl => fl.ListId == listId);

                if (favoritesList == null)
                {
                    _logger.LogWarning($"FavoritesList {listId} not found for User {userId}.");
                    return;
                }

                // Remove the specified NFT from the FavoritesList
                var updatedNfts = favoritesList.Nfts.Where(nft => nft.Identifier != nftIdToDelete).ToList();

                if (updatedNfts.Count == favoritesList.Nfts.Count)
                {
                    _logger.LogWarning($"Nft {nftIdToDelete} not found in FavoritesList {listId} for User {userId}.");
                    return;
                }

                favoritesList.Nfts = updatedNfts;

                // Update the UserFavorites object in DynamoDB
                var updateItemRequest = CreateUpdateItemRequest(userFavorites);
                await _dynamoDbClient.UpdateItemAsync(updateItemRequest);
                
                _logger.LogInformation($"Nft {nftIdToDelete} removed from FavoritesList {listId} for User {userId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing Nft {nftIdToDelete} from FavoritesList {listId} for User {userId}", ex);
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
                        AnimationUrl = nft.M["AnimationUrl"].S
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

        private UpdateItemRequest CreateUpdateItemRequest(UserFavorites userFavorites)
        {
            // TODO add null and empty string checks?
            return new UpdateItemRequest
            {
                TableName = _settings.FavoritesTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userFavorites.UserId } }
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                {
                    {
                        "Favorites", new AttributeValueUpdate
                        {
                            Action = AttributeAction.PUT,
                            Value = new AttributeValue
                            {
                                L = userFavorites.Favorites?
                                    .Where(fl => fl != null &&
                                                !string.IsNullOrEmpty(fl.ListId) &&
                                                !string.IsNullOrEmpty(fl.Name) &&
                                                fl.Nfts != null &&
                                                fl.Nfts.Count > 0)
                                    .Select(fl => new AttributeValue
                                {
                                    M = new Dictionary<string, AttributeValue>
                                    {
                                        { "ListId", new AttributeValue { S = fl.ListId } },
                                        { "Name", new AttributeValue { S = fl.Name } },
                                        { "Nfts", new AttributeValue
                                            {
                                                L = fl.Nfts?
                                                .Where(nft => nft != null &&
                                                            !string.IsNullOrEmpty(nft.Identifier))
                                                .Select(nft => new AttributeValue
                                                {
                                                    M = new Dictionary<string, AttributeValue>
                                                    {
                                                        { "Identifier", new AttributeValue { S = nft.Identifier ?? string.Empty } },
                                                        { "Collection", new AttributeValue { S = nft.Collection ?? string.Empty } },
                                                        { "Contract", new AttributeValue { S = nft.Contract ?? string.Empty } },
                                                        { "Name", new AttributeValue { S = nft.Name ?? string.Empty } },
                                                        { "ImageUrl", new AttributeValue { S = nft.ImageUrl ?? string.Empty } },
                                                        { "AnimationUrl", new AttributeValue { S = nft.AnimationUrl ?? string.Empty } },
                                                    }
                                                }).ToList() ?? new List<AttributeValue>()
                                            }
                                        }
                                    }
                                }).ToList() ?? new List<AttributeValue>()
                            }
                        }
                    }
                }
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
