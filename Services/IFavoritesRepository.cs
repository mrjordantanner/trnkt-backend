using System.Threading.Tasks;
using Trnkt.Models;


namespace Trnkt.Services
{
    public interface IFavoritesRepository
    {
        public Task<UserFavorites> GetFavoritesAsync(string userId);
        public Task<UserFavorites> UpdateFavoritesAsync(string userId, FavoritesList[] lists);
        public Task<bool> DeleteUserFavoritesAsync(string userId);
        public Task<bool> DeleteFavoritesListAsync(string userId, string listIdToDelete);
        public Task<bool> DeleteNftFromFavoritesListAsync(string userId, string listId, string nftIdToDelete);
    }
}