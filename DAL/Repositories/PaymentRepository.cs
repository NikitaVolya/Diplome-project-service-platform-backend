using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Payment?> GetByExternalTransactionIdAsync(string externalTransactionId)
        {
            return await _dbSet
                .Include(p => p.Order)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ExternalTransactionId == externalTransactionId);
        }

        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(p => p.Order)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsOrderPaidAsync(int orderId)
        {
            return await _dbSet.AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Completed);
        }

        public async Task UpdateStatusAsync(int paymentId, PaymentStatus newStatus, DateTime? paidAt = null)
        {
            var payment = await _dbSet.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = newStatus;
                if (paidAt.HasValue)
                {
                    payment.PaidAt = paidAt.Value;
                }
            }
        }

        public async Task<bool> TryUpdateStatusAsync(
            int paymentId,
            PaymentStatus expectedStatus,
            PaymentStatus newStatus,
            DateTime? paidAt = null,
            string? externalTransactionId = null)
        {
            var affectedRows = await _dbSet
                .Where(p => p.Id == paymentId && p.Status == expectedStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, newStatus)
                    .SetProperty(p => p.PaidAt, p => paidAt ?? p.PaidAt)
                    .SetProperty(p => p.ExternalTransactionId, p => externalTransactionId ?? p.ExternalTransactionId));

            return affectedRows > 0;
        }

        public async Task<Payment?> GetLatestPendingByProviderAsync(PaymentProvider provider)
        {
            // Provider defaults to the first enum value for payments that never reached a checkout,
            // so a non-empty ExternalTransactionId is what proves that a checkout really happened.
            return await _dbSet
                .Where(p => p.Provider == provider
                            && p.Status == PaymentStatus.Pending
                            && p.ExternalTransactionId != "")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}