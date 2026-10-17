using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class FavoriteRepository : GenericRepository<Favorite>, IFavoriteRepository
    {
        public FavoriteRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Favorite>> GetFavoriteOrdersByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(f => f.TargetOrder)
                    .ThenInclude(o => o.Category)
                .Where(f => f.UserId == userId && f.TargetOrderId != null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Favorite>> GetFavoriteExecutorsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(f => f.TargetExecutor)
                .Where(f => f.UserId == userId && f.TargetExecutorId != null)
                .ToListAsync();
        }

        public async Task<bool> IsOrderFavoriteAsync(string userId, int orderId)
        {
            return await _dbSet.AnyAsync(f => f.UserId == userId && f.TargetOrderId == orderId);
        }

        public async Task<bool> IsExecutorFavoriteAsync(string userId, string executorId)
        {
            return await _dbSet.AnyAsync(f => f.UserId == userId && f.TargetExecutorId == executorId);
        }

        public async Task RemoveOrderFromFavoritesAsync(string userId, int orderId)
        {
            var favorite = await _dbSet.FirstOrDefaultAsync(f => f.UserId == userId && f.TargetOrderId == orderId);
            if (favorite != null)
            {
                _dbSet.Remove(favorite);
            }
        }

        public async Task RemoveExecutorFromFavoritesAsync(string userId, string executorId)
        {
            var favorite = await _dbSet.FirstOrDefaultAsync(f => f.UserId == userId && f.TargetExecutorId == executorId);
            if (favorite != null)
            {
                _dbSet.Remove(favorite);
            }
        }
    }
}