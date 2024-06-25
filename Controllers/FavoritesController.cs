using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trnkt.Models;
using Trnkt.Services;


namespace Trnkt.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoritesRepository _favoritesRepository;

        public FavoritesController(IFavoritesRepository favoritesRepository)
        {
            _favoritesRepository = favoritesRepository;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFavoritesAsync(string userId)
        {
            var favorites = await _favoritesRepository.GetFavoritesAsync(userId);
            if (favorites == null)
            {
                Console.WriteLine($"ERROR: FavoritesController/GetFavoritesAsync. Favorites for userId {userId} Not Found!");
                return NotFound();
            }
            return Ok(favorites);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> AddToFavoritesAsync(string userId, [FromBody] FavoritesList list)
        {
            if (list == null)
            {
                Console.WriteLine($"ERROR: FavoritesController/AddToFavoritesAsync. FavoritesList from Request Body was null!");
                return BadRequest();
            }

            Console.WriteLine($"FavoritesController/AddToFavoritesAsync.  UserId: {userId}, FavoritesList.Nfts.Count: {list.Nfts.Count}");

            await _favoritesRepository.AddToFavoritesAsync(userId, list);
            return Ok();
        }
    }
}