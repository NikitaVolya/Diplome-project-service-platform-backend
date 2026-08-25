using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByExternalTransactionIdAsync(string externalTransactionId);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId);
        Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId);
        Task<bool> IsOrderPaidAsync(int orderId);

        Task<Payment> CreatePaymentAsync(Payment payment);
        Task ProcessCallbackAsync(string externalTransactionId, PaymentStatus newStatus);
        Task RefundPaymentAsync(int paymentId, string currentUserId);
    }
}