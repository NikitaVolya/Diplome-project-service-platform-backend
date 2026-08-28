using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    public class AdminPaymentService : IAdminPaymentService
    {
        private readonly ApplicationDbContext _db;

        public AdminPaymentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<AdminPaymentListItem>> GetPaymentsAsync(
            PaymentFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await Project(BuildQuery(filter), filter)
                .ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);
        }

        public async Task<IReadOnlyList<AdminPaymentListItem>> GetPaymentsForExportAsync(
            PaymentFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await Project(BuildQuery(filter), filter)
                .Take(10_000)
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminPaymentListItem?> GetPaymentAsync(int id, CancellationToken cancellationToken = default)
        {
            return await Project(_db.Payments.AsNoTracking().Where(p => p.Id == id), new PaymentFilter())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PaymentTotals> GetTotalsAsync(
            PaymentFilter filter,
            CancellationToken cancellationToken = default)
        {
            // Totals must describe the filtered set, ignoring only the status facet so that the
            // four cards can be compared against each other.
            var statusAgnostic = new PaymentFilter
            {
                Search = filter.Search,
                From = filter.From,
                To = filter.To,
                Provider = filter.Provider
            };

            var query = BuildQuery(statusAgnostic);

            var grouped = await query
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new PaymentTotals
            {
                Completed = grouped.FirstOrDefault(g => g.Status == PaymentStatus.Completed)?.Amount ?? 0m,
                Pending = grouped.FirstOrDefault(g => g.Status == PaymentStatus.Pending)?.Amount ?? 0m,
                Failed = grouped.FirstOrDefault(g => g.Status == PaymentStatus.Failed)?.Amount ?? 0m,
                Refunded = grouped.FirstOrDefault(g => g.Status == PaymentStatus.Refunded)?.Amount ?? 0m,
                Count = grouped.Sum(g => g.Count)
            };
        }

        public async Task<AdminOperationResult> MarkRefundedAsync(int id, CancellationToken cancellationToken = default)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (payment == null)
            {
                return AdminOperationResult.Fail("Payment not found.");
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return AdminOperationResult.Fail("Only a completed payment can be refunded.");
            }

            payment.Status = PaymentStatus.Refunded;
            await _db.SaveChangesAsync(cancellationToken);

            return AdminOperationResult.Ok(
                $"Payment #{id} marked as refunded. Issue the actual refund in {payment.Provider}.");
        }

        // -----------------------------------------------------------------

        private IQueryable<Payment> BuildQuery(PaymentFilter filter)
        {
            var query = _db.Payments.AsNoTracking();

            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.Status == filter.Status.Value);
            }

            if (filter.Provider.HasValue)
            {
                query = query.Where(p => p.Provider == filter.Provider.Value);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= filter.From.Value.Date);
            }

            if (filter.ToInclusive.HasValue)
            {
                query = query.Where(p => p.CreatedAt < filter.ToInclusive.Value);
            }

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(p =>
                    p.ExternalTransactionId.Contains(term) ||
                    p.Order.Title.Contains(term) ||
                    p.User.FirstName.Contains(term) ||
                    p.User.LastName.Contains(term));
            }

            return query;
        }

        private static IQueryable<AdminPaymentListItem> Project(IQueryable<Payment> query, PaymentFilter filter)
        {
            var sorted = (filter.SortBy?.ToLowerInvariant(), filter.SortDesc) switch
            {
                ("amount", false) => query.OrderBy(p => p.Amount),
                ("amount", true) => query.OrderByDescending(p => p.Amount),
                ("status", false) => query.OrderBy(p => p.Status),
                ("status", true) => query.OrderByDescending(p => p.Status),
                (_, false) => query.OrderBy(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            return sorted.Select(p => new AdminPaymentListItem
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
            });
        }
    }
}
