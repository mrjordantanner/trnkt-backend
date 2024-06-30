using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trnkt.Models;
using Trnkt.Services;
using System.ComponentModel;


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
        public async Task<IActionResult> GetFavoritesAsync(string userId)
        {
            var favorites = await _favoritesRepository.GetFavoritesAsync(userId);
            if (favorites == null)
            {
                _logger.LogError("FavoritesController/GetFavoritesAsync. Favorites for userId {userId} not found!", userId);
                return NotFound();
            }
            return Ok(favorites);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> UpdateFavoritesAsync(string userId, [FromBody] FavoritesList[] lists)
        {
            if (lists == null)
            {
                _logger.LogError("FavoritesController/UpdateFavoritesAsync. FavoritesLists from Request Body was null!");
                return BadRequest();
            }

            _logger.LogInformation("FavoritesController/UpdateFavoritesAsync.  UserId: {userId}, FavoritesLists.Count: {count}", userId, lists.Length);

            await _favoritesRepository.UpdateFavoritesAsync(userId, lists);
            return Ok();
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteFavoritesAsync(string userId)
        {
            _logger.LogInformation("FavoritesController/DeleteFavoritesAsync. Deleting favorites for userId: {userId}", userId);

            await _favoritesRepository.DeleteFavoritesAsync(userId);
            return Ok();
        }
    }
}