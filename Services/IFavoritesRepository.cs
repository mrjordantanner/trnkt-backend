using System.Threading.Tasks;
using Trnkt.Models;


namespace Trnkt.Services
{
    public interface IFavoritesRepository
    {
        public Task<UserFavorites> GetFavoritesAsync(string userId);
        public Task<UserFavorites> UpdateFavoritesAsync(string userId, FavoritesList[] lists);
        public Task DeleteUserFavoritesAsync(string userId);
        public Task DeleteFavoritesListAsync(string userId, string listIdToDelete);
        public Task DeleteNftFromFavoritesListAsync(string userId, string listId, string nftIdToDelete);
    }
}