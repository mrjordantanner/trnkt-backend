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
            // Return cached data if available
            if (_cache.TryGetValue(userId, out var cachedFavorites))
            {
                Console.WriteLine($"Found userId {userId} in local Cache - Returning cached Favorites.");
                return cachedFavorites; 
            }

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
                var response = await _dynamoDbClient.GetItemAsync(request);
                if (response.Item == null || response.Item.Count == 0)
                {
                    return null;
                }

                var favorites = new UserFavorites
                {
                    UserId = response.Item["UserId"].S,
                    Favorites = response.Item["Favorites"].L.Select(fl => new FavoritesList
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

                _cache[userId] = favorites; // Update cache
                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting Favorites for User {userId}", ex);
                throw;
            }
        }

public async Task UpdateFavoritesAsync(string userId, FavoritesList[] updatedLists)
{
    if (updatedLists == null)
    {
        _logger.LogError($"UpdateFavoritesAsync-- Update failed. Argument 'updatedLists' was null.");
        return;
    }

    bool isModified = false;
    var userFavorites = await GetFavoritesAsync(userId) ?? new UserFavorites { UserId = userId, Favorites = new List<FavoritesList>() };

    foreach (var updatedList in updatedLists)
    {
        var existingList = userFavorites.Favorites.FirstOrDefault(fl => fl.ListId == updatedList.ListId);

        // If the ListId already exists, perform comparisons to check for changes
        if (existingList != null)
        {
            bool nftsAreDifferent = existingList.Nfts.Count != updatedList.Nfts.Count ||
                                    !existingList.Nfts.All(nft => updatedList.Nfts.Any(uNft => uNft.Identifier == nft.Identifier));

            // If changes are found, update the ExistingList's properties with the UpdatedList's properties
            if (existingList.Name != updatedList.Name || nftsAreDifferent)
            {
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
        var putItemRequest = new PutItemRequest
        {
            TableName = _settings.FavoritesTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "UserId", new AttributeValue { S = userFavorites.UserId } },
                { "Favorites", new AttributeValue
                    {
                        L = userFavorites.Favorites.Select(fl => new AttributeValue
                        {
                            M = new Dictionary<string, AttributeValue>
                            {
                                { "ListId", new AttributeValue { S = fl.ListId } },
                                { "Name", new AttributeValue { S = fl.Name } },
                                { "Nfts", new AttributeValue
                                    {
                                        L = fl.Nfts.Select(nft => new AttributeValue
                                        {
                                            M = new Dictionary<string, AttributeValue>
                                            {
                                                { "Identifier", new AttributeValue { S = nft.Identifier } },
                                                { "Collection", new AttributeValue { S = nft.Collection } },
                                                { "Contract", new AttributeValue { S = nft.Contract } },
                                                { "Name", new AttributeValue { S = nft.Name } },
                                                { "ImageUrl", new AttributeValue { S = nft.ImageUrl } },
                                                { "AnimationUrl", new AttributeValue { S = nft.AnimationUrl } }
                                            }
                                        }).ToList()
                                    }
                                }
                            }
                        }).ToList()
                    }
                }
            }
        };

        try
        {
            await _dynamoDbClient.PutItemAsync(putItemRequest);
            _cache[userId] = userFavorites; // Update the cache after successful write
            _logger.LogInformation("Favorites updated successfully for User {userId}", userId);
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
    }
}


        public async Task DeleteFavoritesAsync(string userId)
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

        // If needed, remove cache entry for the given userId so the next read will read from DynamoDb
        public void InvalidateCache(string userId)
        {
            _cache.Remove(userId); 
        }
    }
}