using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> GetForUserAsync(int id, string userId, bool isAdmin);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId);
        Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId, string userId, bool isAdmin);
        Task<bool> IsOrderPaidAsync(int orderId, string userId, bool isAdmin);

        /// <summary>
        /// Creates (or returns the already existing pending) payment for an order.
        /// The amount is taken from the order on the server; the client cannot choose it.
        /// </summary>
        Task<Payment> CreatePaymentAsync(int orderId, string userId);

        Task<PaymentCheckout> CreateCheckoutAsync(
            int paymentId,
            string providerName,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies and applies a provider webhook. Returns the body the provider expects
        /// in the response (WayForPay), or null when a plain 200 is enough.
        /// </summary>
        Task<object?> HandleWebhookAsync(string providerName, GatewayRequest request);

        Task RefundPaymentAsync(int paymentId, string currentUserId);
    }
}
