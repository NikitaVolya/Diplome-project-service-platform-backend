using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    public class AdminOrderService : IAdminOrderService
    {
        private readonly ApplicationDbContext _db;

        public AdminOrderService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<AdminOrderListItem>> GetOrdersAsync(
            OrderFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await Project(BuildQuery(filter), filter)
                .ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);
        }

        public async Task<IReadOnlyList<AdminOrderListItem>> GetOrdersForExportAsync(
            OrderFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await Project(BuildQuery(filter), filter)
                .Take(10_000)
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminOrderDetails?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => new AdminOrderDetails
                {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Price = o.Price,
                    Status = o.Status,
                    Address = o.Address,
                    CategoryId = o.CategoryId,
                    CategoryName = o.Category.Name,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    ExecutorId = o.ExecutorId,
                    ExecutorName = o.Executor == null ? null : o.Executor.FirstName + " " + o.Executor.LastName,
                    CreatedAt = o.CreatedAt,
                    ExecutionAt = o.ExecutionAt,
                    ApplicationsCount = o.Applications.Count,
                    MessagesCount = o.OrderMessages.Count
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                return null;
            }

            order.Applications = await _db.Applications
                .AsNoTracking()
                .Where(a => a.OrderId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AdminApplicationListItem
                {
                    Id = a.Id,
                    ExecutorId = a.ExecutorId,
                    ExecutorName = a.Executor == null
                        ? "—"
                        : a.Executor.FirstName + " " + a.Executor.LastName,
                    ProposedPrice = a.ProposedPrice,
                    Comment = a.Comment,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            order.Payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.OrderId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new AdminPaymentListItem
                {
                    Id = p.Id,
                    OrderId = p.OrderId,
                    OrderTitle = p.Order.Title,
                    UserId = p.UserId,
                    UserName = p.User.FirstName + " " + p.User.LastName,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Provider = p.Provider,
                    Status = p.Status,
                    ExternalTransactionId = p.ExternalTransactionId,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt
                })
                .ToListAsync(cancellationToken);

            order.Reviews = await _db.Reviews
                .AsNoTracking()
                .Where(r => r.OrderId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AdminReviewListItem
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
                })
                .ToListAsync(cancellationToken);

            return order;
        }

        public async Task<AdminOperationResult> ChangeStatusAsync(
            int id,
            OrderStatus status,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order == null)
            {
                return AdminOperationResult.Fail("Order not found.");
            }

            if (order.Status == status)
            {
                return AdminOperationResult.Fail($"Order is already {status}.");
            }

            if (status == OrderStatus.InProgress && order.ExecutorId == null)
            {
                return AdminOperationResult.Fail("An order cannot be in progress without an executor.");
            }

            var previous = order.Status;
            order.Status = status;

            await _db.SaveChangesAsync(cancellationToken);
            return AdminOperationResult.Ok($"Order #{id}: {previous} → {status}.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order == null)
            {
                return AdminOperationResult.Fail("Order not found.");
            }

            var hasPayments = await _db.Payments.AnyAsync(
                p => p.OrderId == id && p.Status == PaymentStatus.Completed, cancellationToken);

            if (hasPayments)
            {
                return AdminOperationResult.Fail(
                    "This order has completed payments and cannot be deleted. Cancel it instead.");
            }

            // Reviews and payments point at the order without cascade on the principal side,
            // so remove them explicitly before deleting the order itself.
            var reviews = await _db.Reviews.Where(r => r.OrderId == id).ToListAsync(cancellationToken);
            _db.Reviews.RemoveRange(reviews);

            var payments = await _db.Payments.Where(p => p.OrderId == id).ToListAsync(cancellationToken);
            _db.Payments.RemoveRange(payments);

            var favorites = await _db.Favorites.Where(f => f.TargetOrderId == id).ToListAsync(cancellationToken);
            _db.Favorites.RemoveRange(favorites);

            _db.Orders.Remove(order);

            await _db.SaveChangesAsync(cancellationToken);
            return AdminOperationResult.Ok($"Order #{id} deleted.");
        }

        // -----------------------------------------------------------------

        private IQueryable<Order> BuildQuery(OrderFilter filter)
        {
            var query = _db.Orders.AsNoTracking();

            if (filter.Status.HasValue)
            {
                query = query.Where(o => o.Status == filter.Status.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(o => o.CategoryId == filter.CategoryId.Value);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(o => o.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(o => o.Price <= filter.MaxPrice.Value);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= filter.From.Value.Date);
            }

            if (filter.ToInclusive.HasValue)
            {
                query = query.Where(o => o.CreatedAt < filter.ToInclusive.Value);
            }

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(o =>
                    o.Title.Contains(term) ||
                    o.Description.Contains(term) ||
                    o.Address.Contains(term) ||
                    o.Customer.FirstName.Contains(term) ||
                    o.Customer.LastName.Contains(term));
            }

            return query;
        }

        private static IQueryable<AdminOrderListItem> Project(IQueryable<Order> query, OrderFilter filter)
        {
            var sorted = (filter.SortBy?.ToLowerInvariant(), filter.SortDesc) switch
            {
                ("price", false) => query.OrderBy(o => o.Price),
                ("price", true) => query.OrderByDescending(o => o.Price),
                ("title", false) => query.OrderBy(o => o.Title),
                ("title", true) => query.OrderByDescending(o => o.Title),
                ("status", false) => query.OrderBy(o => o.Status),
                ("status", true) => query.OrderByDescending(o => o.Status),
                (_, false) => query.OrderBy(o => o.CreatedAt),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            return sorted.Select(o => new AdminOrderListItem
            {
                Id = o.Id,
                Title = o.Title,
                Price = o.Price,
                Status = o.Status,
                CategoryId = o.CategoryId,
                CategoryName = o.Category.Name,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                ExecutorId = o.ExecutorId,
                ExecutorName = o.Executor == null ? null : o.Executor.FirstName + " " + o.Executor.LastName,
                Address = o.Address,
                CreatedAt = o.CreatedAt,
                ExecutionAt = o.ExecutionAt,
                ApplicationsCount = o.Applications.Count
            });
        }
    }
}
