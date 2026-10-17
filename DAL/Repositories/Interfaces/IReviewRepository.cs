using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<IEnumerable<Review>> GetByTargetUserIdAsync(string targetUserId);

        Task<IEnumerable<Review>> GetByAuthorIdAsync(string authorId);

        Task<Review?> GetByOrderIdAsync(int orderId);

        Task<double> GetAverageRatingForUserAsync(string userId);

        Task<bool> HasReviewForOrderAsync(int orderId, string authorId);
    }
}