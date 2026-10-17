using BLL.Services.Interfaces;
using Domain.Models;
using DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.UnitOfWork.Interfaces;

namespace BLL.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public FavoriteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Favorite>> GetFavoriteOrdersAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            return await _unitOfWork.Favorites.GetFavoriteOrdersByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Favorite>> GetFavoriteExecutorsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            return await _unitOfWork.Favorites.GetFavoriteExecutorsByUserIdAsync(userId);
        }

        public async Task<bool> IsOrderFavoriteAsync(string userId, int orderId)
        {
            return await _unitOfWork.Favorites.IsOrderFavoriteAsync(userId, orderId);
        }

        public async Task<bool> IsExecutorFavoriteAsync(string userId, string executorId)
        {
            return await _unitOfWork.Favorites.IsExecutorFavoriteAsync(userId, executorId);
        }

        public async Task AddOrderToFavoritesAsync(string userId, int orderId)
        {
            var isFavorite = await _unitOfWork.Favorites.IsOrderFavoriteAsync(userId, orderId);
            if (isFavorite)
                return;

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} does not exist.", nameof(orderId));
            }

            var favorite = new Favorite
            {
                UserId = userId,
                TargetOrderId = orderId
            };

            await _unitOfWork.Favorites.AddAsync(favorite);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveOrderFromFavoritesAsync(string userId, int orderId)
        {
            await _unitOfWork.Favorites.RemoveOrderFromFavoritesAsync(userId, orderId);
        }

        public async Task<bool> ToggleFavoriteOrderAsync(string userId, int orderId)
        {
            var isFavorite = await _unitOfWork.Favorites.IsOrderFavoriteAsync(userId, orderId);
            if (isFavorite)
            {
                await RemoveOrderFromFavoritesAsync(userId, orderId);
                return false;
            }
            else
            {
                await AddOrderToFavoritesAsync(userId, orderId);
                return true;
            }
        }

        public async Task AddExecutorToFavoritesAsync(string userId, string executorId)
        {
            if (userId == executorId)
            {
                throw new ArgumentException("User cannot favorite themselves.", nameof(executorId));
            }

            var isFavorite = await _unitOfWork.Favorites.IsExecutorFavoriteAsync(userId, executorId);
            if (isFavorite)
                return;

            var favorite = new Favorite
            {
                UserId = userId,
                TargetExecutorId = executorId
            };

            await _unitOfWork.Favorites.AddAsync(favorite);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveExecutorFromFavoritesAsync(string userId, string executorId)
        {
            await _unitOfWork.Favorites.RemoveExecutorFromFavoritesAsync(userId, executorId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> ToggleFavoriteExecutorAsync(string userId, string executorId)
        {
            var isFavorite = await _unitOfWork.Favorites.IsExecutorFavoriteAsync(userId, executorId);
            if (isFavorite)
            {
                await RemoveExecutorFromFavoritesAsync(userId, executorId);
                return false;
            }
            else
            {
                await AddExecutorToFavoritesAsync(userId, executorId);
                return true;
            }
        }
    }
}
