using BLL.Services.Interfaces;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Reviews.GetByIdAsync(id);
        }

        public async Task<Review?> GetByOrderIdAsync(int orderId)
        {
            return await _unitOfWork.Reviews.GetByOrderIdAsync(orderId);
        }

        public async Task<IEnumerable<Review>> GetUserReviewsAsync(string targetUserId)
        {
            if (string.IsNullOrEmpty(targetUserId))
            {
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));
            }
            return await _unitOfWork.Reviews.GetByTargetUserIdAsync(targetUserId);
        }

        public async Task<IEnumerable<Review>> GetAuthoredReviewsAsync(string authorId)
        {
            if (string.IsNullOrEmpty(authorId))
            {
                throw new ArgumentException("Author ID cannot be null or empty.", nameof(authorId));
            }
            return await _unitOfWork.Reviews.GetByAuthorIdAsync(authorId);
        }

        public async Task<double> GetAverageRatingAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return 0.0;

            return await _unitOfWork.Reviews.GetAverageRatingForUserAsync(userId);
        }

        public async Task<Review> CreateReviewAsync(Review review)
        {
            if (review.Rating < 1 || review.Rating > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(review.Rating), "Rating must be between 1 and 5.");
            }

            if (string.IsNullOrWhiteSpace(review.Comment))
            {
                throw new ArgumentException("Comment cannot be null or whitespace.", nameof(review.Comment));
            }

            if (review.AuthorId == review.TargetUserId)
            {
                throw new InvalidOperationException("A user cannot review themselves.");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(review.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order does not exist.");
            }

            if (order.Status != OrderStatus.Completed)
            {
                throw new InvalidOperationException("Cannot create a review for an order that is not completed.");
            }

            if (order.CustomerId != review.AuthorId && order.ExecutorId != review.AuthorId)
            {
                throw new InvalidOperationException("Only the customer or executor of the order can create a review.");
            }

            var alreadyReviewed = await _unitOfWork.Reviews.HasReviewForOrderAsync(review.OrderId, review.AuthorId);
            if (alreadyReviewed)
            {
                throw new InvalidOperationException("A review for this order by this author already exists.");
            }

            review.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            return review;
        }

        public async Task UpdateReviewAsync(Review review, string currentUserId)
        {
            var existingReview = await _unitOfWork.Reviews.GetByIdAsync(review.Id);

            if (existingReview == null)
            {
                throw new InvalidOperationException("Review does not exist.");
            }

            if (existingReview.AuthorId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this review.");
            }

            if (review.Rating < 1 || review.Rating > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(review.Rating), "Rating must be between 1 and 5.");
            }

            if (string.IsNullOrWhiteSpace(review.Comment))
            {
                throw new ArgumentException("Comment cannot be null or whitespace.", nameof(review.Comment));
            }

            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;

            await _unitOfWork.Reviews.UpdateAsync(existingReview);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteReviewAsync(int reviewId, string currentUserId)
        {
            var existingReview = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (existingReview == null)
            {
                throw new InvalidOperationException("Review does not exist.");
            }
            if (existingReview.AuthorId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this review.");
            }
            await _unitOfWork.Reviews.DeleteAsync(existingReview);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
