using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    /// <summary>
    /// Скарги та відгуки — контент, з яким працює адміністратор. Скарги зберігають історію
    /// (змінюється лише статус), відгуки видаляються повністю, бо в моделі немає прапорця «прихований».
    /// </summary>
    public class AdminModerationService : IAdminModerationService
    {
        /// <summary>Замовлення без руху довше за цей термін потрапляє в чергу модерації.</summary>
        private const int StaleOrderDays = 30;

        private readonly ApplicationDbContext _db;

        public AdminModerationService(ApplicationDbContext db)
        {
            _db = db;
        }

        // -----------------------------------------------------------------
        // Скарги
        // -----------------------------------------------------------------

        public async Task<PagedResult<AdminComplaintListItem>> GetComplaintsAsync(
            ComplaintFilter filter,
            CancellationToken cancellationToken = default)
        {
            var page = await ProjectComplaints(BuildComplaintQuery(filter), filter)
                .ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);

            await AttachComplaintCountsAsync(page.Items, cancellationToken);
            return page;
        }

        public async Task<IReadOnlyList<AdminComplaintListItem>> GetComplaintsForExportAsync(
            ComplaintFilter filter,
            CancellationToken cancellationToken = default)
        {
            var items = await ProjectComplaints(BuildComplaintQuery(filter), filter)
                .Take(10_000)
                .ToListAsync(cancellationToken);

            await AttachComplaintCountsAsync(items, cancellationToken);
            return items;
        }

        public async Task<AdminComplaintListItem?> GetComplaintAsync(int id, CancellationToken cancellationToken = default)
        {
            var complaint = await _db.Complaints
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new AdminComplaintListItem
                {
                    Id = c.Id,
                    SenderId = c.SenderId,
                    SenderName = c.Sender.FirstName + " " + c.Sender.LastName,
                    TargetUserId = c.TargetUserId,
                    TargetUserName = c.TargetUser == null ? null : c.TargetUser.FirstName + " " + c.TargetUser.LastName,
                    Reason = c.Reason,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (complaint != null)
            {
                await AttachComplaintCountsAsync(new[] { complaint }, cancellationToken);
            }

            return complaint;
        }

        public async Task<AdminOperationResult> SetComplaintStatusAsync(
            int id,
            ComplaintStatus status,
            CancellationToken cancellationToken = default)
        {
            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (complaint == null)
            {
                return AdminOperationResult.Fail("Complaint not found.");
            }

            if (complaint.Status == status)
            {
                return AdminOperationResult.Fail($"Complaint is already {status}.");
            }

            complaint.Status = status;
            await _db.SaveChangesAsync(cancellationToken);

            return AdminOperationResult.Ok($"Complaint #{id} marked as {status}.");
        }

        // -----------------------------------------------------------------
        // Відгуки
        // -----------------------------------------------------------------

        public async Task<PagedResult<AdminReviewListItem>> GetReviewsAsync(
            ReviewFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await ProjectReviews(BuildReviewQuery(filter), filter)
                .ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);
        }

        public async Task<IReadOnlyList<AdminReviewListItem>> GetReviewsForExportAsync(
            ReviewFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await ProjectReviews(BuildReviewQuery(filter), filter)
                .Take(10_000)
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminOperationResult> DeleteReviewAsync(int id, CancellationToken cancellationToken = default)
        {
            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (review == null)
            {
                return AdminOperationResult.Fail("Review not found.");
            }

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync(cancellationToken);

            return AdminOperationResult.Ok($"Review #{id} removed.");
        }

        // -----------------------------------------------------------------
        // Черга
        // -----------------------------------------------------------------

        public async Task<ModerationQueue> GetQueueAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var staleBefore = DateTime.UtcNow.AddDays(-StaleOrderDays);

            return new ModerationQueue
            {
                PendingComplaints = await _db.Complaints
                    .CountAsync(c => c.Status == ComplaintStatus.Pending, cancellationToken),

                LowRatedReviews = await _db.Reviews
                    .CountAsync(r => r.Rating <= 2, cancellationToken),

                BlockedUsers = await _db.Users
                    .CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > now, cancellationToken),

                StaleOrders = await _db.Orders
                    .CountAsync(o => o.Status == OrderStatus.Pending && o.CreatedAt < staleBefore, cancellationToken),

                UnpaidCompletedOrders = await _db.Orders
                    .CountAsync(o => o.Status == OrderStatus.Completed
                                     && !_db.Payments.Any(p => p.OrderId == o.Id && p.Status == PaymentStatus.Completed),
                        cancellationToken)
            };
        }

        // -----------------------------------------------------------------

        private IQueryable<Complaint> BuildComplaintQuery(ComplaintFilter filter)
        {
            var query = _db.Complaints.AsNoTracking();

            if (filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == filter.Status.Value);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= filter.From.Value.Date);
            }

            if (filter.ToInclusive.HasValue)
            {
                query = query.Where(c => c.CreatedAt < filter.ToInclusive.Value);
            }

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(c =>
                    c.Reason.Contains(term) ||
                    c.Description.Contains(term) ||
                    c.Sender.FirstName.Contains(term) ||
                    c.Sender.LastName.Contains(term) ||
                    (c.TargetUser != null && (c.TargetUser.FirstName.Contains(term) || c.TargetUser.LastName.Contains(term))));
            }

            return query;
        }

        private static IQueryable<AdminComplaintListItem> ProjectComplaints(
            IQueryable<Complaint> query,
            ComplaintFilter filter)
        {
            var sorted = filter.SortDesc
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt);

            return sorted.Select(c => new AdminComplaintListItem
            {
                Id = c.Id,
                SenderId = c.SenderId,
                SenderName = c.Sender.FirstName + " " + c.Sender.LastName,
                TargetUserId = c.TargetUserId,
                TargetUserName = c.TargetUser == null ? null : c.TargetUser.FirstName + " " + c.TargetUser.LastName,
                Reason = c.Reason,
                Description = c.Description,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            });
        }

        /// <summary>
        /// Додає «скільки всього скарг зібрав цей користувач» — саме це число насправді визначає,
        /// блокувати його чи ні.
        /// </summary>
        private async Task AttachComplaintCountsAsync(
            IReadOnlyList<AdminComplaintListItem> items,
            CancellationToken cancellationToken)
        {
            var targetIds = items
                .Where(i => i.TargetUserId != null)
                .Select(i => i.TargetUserId!)
                .Distinct()
                .ToList();

            if (targetIds.Count == 0)
            {
                return;
            }

            var counts = await _db.Complaints
                .Where(c => c.TargetUserId != null && targetIds.Contains(c.TargetUserId))
                .GroupBy(c => c.TargetUserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

            foreach (var item in items)
            {
                if (item.TargetUserId != null && counts.TryGetValue(item.TargetUserId, out var count))
                {
                    item.TargetComplaintCount = count;
                }
            }
        }

        private IQueryable<Review> BuildReviewQuery(ReviewFilter filter)
        {
            var query = _db.Reviews.AsNoTracking();

            if (filter.MinRating.HasValue)
            {
                query = query.Where(r => r.Rating >= filter.MinRating.Value);
            }

            if (filter.MaxRating.HasValue)
            {
                query = query.Where(r => r.Rating <= filter.MaxRating.Value);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= filter.From.Value.Date);
            }

            if (filter.ToInclusive.HasValue)
            {
                query = query.Where(r => r.CreatedAt < filter.ToInclusive.Value);
            }

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(r =>
                    r.Comment.Contains(term) ||
                    r.Order.Title.Contains(term) ||
                    r.Author.FirstName.Contains(term) ||
                    r.Author.LastName.Contains(term) ||
                    r.TargetUser.FirstName.Contains(term) ||
                    r.TargetUser.LastName.Contains(term));
            }

            return query;
        }

        private static IQueryable<AdminReviewListItem> ProjectReviews(IQueryable<Review> query, ReviewFilter filter)
        {
            var sorted = (filter.SortBy?.ToLowerInvariant(), filter.SortDesc) switch
            {
                ("rating", false) => query.OrderBy(r => r.Rating),
                ("rating", true) => query.OrderByDescending(r => r.Rating),
                (_, false) => query.OrderBy(r => r.CreatedAt),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            return sorted.Select(r => new AdminReviewListItem
            {
                Id = r.Id,
                OrderId = r.OrderId,
                OrderTitle = r.Order.Title,
                AuthorId = r.AuthorId,
                AuthorName = r.Author.FirstName + " " + r.Author.LastName,
                TargetUserId = r.TargetUserId,
                TargetUserName = r.TargetUser.FirstName + " " + r.TargetUser.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            });
        }
    }
}
