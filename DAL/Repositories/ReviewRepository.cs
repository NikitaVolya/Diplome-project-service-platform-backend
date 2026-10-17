using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetByTargetUserIdAsync(string targetUserId)
        {
            return await _dbSet
                .Include(r => r.Author)
                .Include(r => r.Order)
                .Where(r => r.TargetUserId == targetUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetByAuthorIdAsync(string authorId)
        {
            return await _dbSet
                .Include(r => r.TargetUser)
                .Include(r => r.Order)
                .Where(r => r.AuthorId == authorId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Include(r => r.Author)
                .Include(r => r.TargetUser)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<double> GetAverageRatingForUserAsync(string userId)
        {
            var reviews = _dbSet.Where(r => r.TargetUserId == userId);

            if (!await reviews.AnyAsync())
            {
                return 0.0;
            }

            return await reviews.AverageAsync(r => r.Rating);
        }

        public async Task<bool> HasReviewForOrderAsync(int orderId, string authorId)
        {
            return await _dbSet.AnyAsync(r => r.OrderId == orderId && r.AuthorId == authorId);
        }
    }
}