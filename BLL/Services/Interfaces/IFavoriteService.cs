using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<IEnumerable<Favorite>> GetFavoriteOrdersAsync(string userId);
        Task<IEnumerable<Favorite>> GetFavoriteExecutorsAsync(string userId);
        Task<bool> IsOrderFavoriteAsync(string userId, int orderId);
        Task<bool> IsExecutorFavoriteAsync(string userId, string executorId);

        Task AddOrderToFavoritesAsync(string userId, int orderId);
        Task RemoveOrderFromFavoritesAsync(string userId, int orderId);
        Task<bool> ToggleFavoriteOrderAsync(string userId, int orderId); // Возвращает true, если добавлен, false — если удален

        Task AddExecutorToFavoritesAsync(string userId, string executorId);
        Task RemoveExecutorFromFavoritesAsync(string userId, string executorId);
        Task<bool> ToggleFavoriteExecutorAsync(string userId, string executorId);
    }
}