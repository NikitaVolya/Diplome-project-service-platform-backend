using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IFavoriteRepository : IGenericRepository<Favorite>
    {
        Task<IEnumerable<Favorite>> GetFavoriteOrdersByUserIdAsync(string userId);

        Task<IEnumerable<Favorite>> GetFavoriteExecutorsByUserIdAsync(string userId);

        Task<bool> IsOrderFavoriteAsync(string userId, int orderId);

        Task<bool> IsExecutorFavoriteAsync(string userId, string executorId);

        Task RemoveOrderFromFavoritesAsync(string userId, int orderId);

        Task RemoveExecutorFromFavoritesAsync(string userId, string executorId);
    }
}