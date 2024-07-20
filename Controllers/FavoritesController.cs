using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trnkt.Models;
using Trnkt.Services;
using System.ComponentModel;
using System.Linq;


namespace Trnkt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoritesRepository _favoritesRepository;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(IFavoritesRepository favoritesRepository, ILogger<FavoritesController> logger)
        {
            _favoritesRepository = favoritesRepository;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        //[Authorize]
        public async Task<IActionResult> GetFavoritesAsync(string userId)
        {
            var favorites = await _favoritesRepository.GetFavoritesAsync(userId);
            if (favorites == null)
            {
                _logger.LogInformation("GetFavoritesAsync: UserFavorites for userId {userId} not found.", userId);
                return NotFound();
            }
            return Ok(favorites);
        }

        // Update Favorites STEP 4
        [HttpPost("{userId}")]
        //[Authorize]
        public async Task<IActionResult> UpdateFavoritesAsync(string userId, [FromBody] FavoritesList[] lists)
        {
            if (lists == null)
            {
                _logger.LogError("UpdateFavoritesAsync: FavoritesLists from Request Body was null!");
                return BadRequest();
            }

            if (lists.Length == 0)
            {
                _logger.LogError("UpdateFavoritesAsync: FavoritesLists from Request Body was empty!");
                return BadRequest();
            }

            // TODO Note: ImageUrls are already null at this point- ---
            _logger.LogInformation("UpdateFavoritesAsync:  UserId: {userId}, FavoritesLists.Count: {count}", userId, lists.Length);
        //     foreach (var list in lists)
        //     {
        //         _logger.LogInformation($"- {list.Name}");

        //         // TODO Note: ImageUrl is NULL for the new NFT being saved, but EMPTY STRINGS for the other existing NFTs on the list
        //         // This is because we are saving the NFTs with ImageUrls as null, then they get written to DynamoDb as an empty string
        //         // SO, when they get read from DynamoDb and come back, they are empty strings now 
        //         // So main question still remains, why is ImageUrl NULL here but other values from the request body aren't?
        //         foreach (var nft in list.Nfts)
        //         {
        //             string imgUrl = nft.ImageUrl != "" ? nft.ImageUrl : "EMPTY STRING";
        //             imgUrl ??= "NULL STRING";
        //             _logger.LogInformation($"  - {nft.Name}: {imgUrl}");
        //         }
        //     }

            var updatedFavorites = await _favoritesRepository.UpdateFavoritesAsync(userId, lists);

            // Update Favorites STEP 6
            return Ok(updatedFavorites);
        }

        [HttpDelete("{userId}")]
        //[Authorize]
        public async Task<IActionResult> DeleteUserFavoritesAsync(string userId)
        {
            _logger.LogInformation("DeleteUserFavoritesAsync:  Deleting UserFavorites for userId: {userId}", userId);

            await _favoritesRepository.DeleteUserFavoritesAsync(userId);
            return Ok();
        }

        [HttpDelete("{userId}/{listId}")]
        //[Authorize]
        public async Task<IActionResult> DeleteFavoritesListAsync(string userId, string listId)
        {
            _logger.LogInformation("DeleteFavoritesListAsync:  Attempting to DELETE FavoritesList {listId} for userId: {userId}", listId, userId);

            await _favoritesRepository.DeleteFavoritesListAsync(userId, listId);
            return Ok();
        }

        [HttpDelete("{userId}/{listId}/{nftId}")]
        //[Authorize]
        public async Task<IActionResult> DeleteNftFromFavoritesListAsync(string userId, string listId, string nftId)
        {
            _logger.LogInformation("DeleteNftFromFavoritesList:  Attempting to DELETE Nft {nftId} from FavoritesList {listId} for userId: {userId}", nftId, listId, userId);

            await _favoritesRepository.DeleteNftFromFavoritesListAsync(userId, listId, nftId);
            return Ok();
        }
    }
}