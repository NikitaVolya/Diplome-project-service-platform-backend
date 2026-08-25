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
    }
}