using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByExternalTransactionIdAsync(string externalTransactionId);

        Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId);

        Task<IEnumerable<Payment>> GetByUserIdAsync(string userId);

        Task<bool> IsOrderPaidAsync(int orderId);

        Task UpdateStatusAsync(int paymentId, PaymentStatus newStatus, DateTime? paidAt = null);

        /// <summary>
        /// Atomic "compare and set": changes the status only if it is still expectedStatus.
        /// Returns false when somebody else already changed it (e.g. a duplicate webhook).
        /// Executes immediately in the database; no SaveChanges is needed.
        /// </summary>
        Task<bool> TryUpdateStatusAsync(
            int paymentId,
            PaymentStatus expectedStatus,
            PaymentStatus newStatus,
            DateTime? paidAt = null,
            string? externalTransactionId = null);

        /// <summary>
        /// Most recent pending payment of the given provider that already started a checkout
        /// (i.e. has a provider-side id). Used only to map provider TEST events in development.
        /// </summary>
        Task<Payment?> GetLatestPendingByProviderAsync(PaymentProvider provider);
    }
}