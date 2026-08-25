using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IReviewService
    {
        Task<Review?> GetByIdAsync(int id);
        Task<Review?> GetByOrderIdAsync(int orderId);
        Task<IEnumerable<Review>> GetUserReviewsAsync(string targetUserId);
        Task<IEnumerable<Review>> GetAuthoredReviewsAsync(string authorId);
        Task<double> GetAverageRatingAsync(string userId);

        Task<Review> CreateReviewAsync(Review review);
        Task UpdateReviewAsync(Review review, string currentUserId);
        Task DeleteReviewAsync(int reviewId, string currentUserId);
    }
}