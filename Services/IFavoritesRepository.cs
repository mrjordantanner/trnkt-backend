using System.Threading.Tasks;
using Trnkt.Models;


namespace Trnkt.Services
{
    public interface IFavoritesRepository
    {
        public Task<UserFavorites> GetFavoritesAsync(string userId);
        public Task<UserFavorites> UpdateFavoritesAsync(string userId, FavoritesList[] lists);
        public Task DeleteFavoritesAsync(string userId);
    }
}