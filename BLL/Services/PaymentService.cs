using BLL.Exceptions;
using BLL.Options;
using BLL.Services.Interfaces;
using BLL.Services.PaymentGateways;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly PaymentGatewayFactory _gatewayFactory;
        private readonly PaymentOptions _options;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            PaymentGatewayFactory gatewayFactory,
            IOptions<PaymentOptions> options,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _gatewayFactory = gatewayFactory;
            _options = options.Value;
            _logger = logger;
        }

        // ------------------------------------------------------------------ queries

        public async Task<Payment> GetForUserAsync(int id, string userId, bool isAdmin)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(id);
            if (payment == null)
            {
                throw new NotFoundException($"Payment with ID {id} not found.");
            }

            if (!isAdmin && payment.UserId != userId)
            {
                throw new ForbiddenException("You do not have access to this payment.");
            }

            return payment;
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationFailedException("User ID cannot be null or empty.");
            }
            return await _unitOfWork.Payments.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId, string userId, bool isAdmin)
        {
            await EnsureCanViewOrderAsync(orderId, userId, isAdmin);
            return await _unitOfWork.Payments.GetByOrderIdAsync(orderId);
        }

        public async Task<bool> IsOrderPaidAsync(int orderId, string userId, bool isAdmin)
        {
            await EnsureCanViewOrderAsync(orderId, userId, isAdmin);
            return await _unitOfWork.Payments.IsOrderPaidAsync(orderId);
        }

        // ------------------------------------------------------------------ creating a payment

        public async Task<Payment> CreatePaymentAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException($"Order with ID {orderId} does not exist.");
            }

            // Only the customer of the order may pay for it.
            if (order.CustomerId != userId)
            {
                throw new ForbiddenException("You can only pay for your own orders.");
            }

            if (order.Status != OrderStatus.InProgress)
            {
                throw new ConflictException("Only orders that are in progress can be paid.");
            }

            if (order.Price <= 0)
            {
                throw new ConflictException("The order price must be greater than zero before it can be paid.");
            }

            if (await _unitOfWork.Payments.IsOrderPaidAsync(orderId))
            {
                throw new ConflictException($"Order with ID {orderId} has already been paid.");
            }

            // Re-use an unfinished payment instead of piling up duplicates.
            var existing = (await _unitOfWork.Payments.GetByOrderIdAsync(orderId))
                .FirstOrDefault(p => p.Status == PaymentStatus.Pending && p.UserId == userId);

            if (existing != null)
            {
                if (existing.Amount != order.Price)
                {
                    existing.Amount = order.Price;
                    await _unitOfWork.SaveChangesAsync();
                }
                return existing;
            }

            var payment = new Payment
            {
                OrderId = orderId,
                UserId = userId,
                // The amount always comes from the order, never from the client.
                Amount = order.Price,
                Currency = _options.Currency,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                PaidAt = null
            };

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return payment;
        }

        public async Task<PaymentCheckout> CreateCheckoutAsync(
            int paymentId,
            string providerName,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var gateway = _gatewayFactory.GetGateway(providerName);

            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new NotFoundException($"Payment with ID {paymentId} not found.");
            }

            if (payment.UserId != userId)
            {
                throw new ForbiddenException("You do not have access to this payment.");
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                throw new ConflictException($"Payment is already {payment.Status} and cannot be paid.");
            }

            // URLs come from configuration, not from code.
            var callbackUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/payments/webhook/{gateway.ProviderName.ToLowerInvariant()}";

            var checkout = await gateway.CreateCheckoutAsync(payment, _options.ReturnUrl, callbackUrl, cancellationToken);

            payment.Provider = gateway.Provider;
            if (!string.IsNullOrEmpty(checkout.ExternalId))
            {
                payment.ExternalTransactionId = checkout.ExternalId;
            }
            await _unitOfWork.SaveChangesAsync();

            return checkout;
        }

        // ------------------------------------------------------------------ webhooks

        public async Task<object?> HandleWebhookAsync(string providerName, GatewayRequest request)
        {
            var gateway = _gatewayFactory.GetGateway(providerName);

            GatewayCallback? callback;
            try
            {
                // Throws UnauthorizedException when the signature does not match.
                callback = gateway.ParseCallback(request);
            }
            catch (UnauthorizedException)
            {
                _logger.LogWarning("Rejected {Provider} webhook: invalid signature.", gateway.ProviderName);
                throw;
            }

            if (callback == null)
            {
                return null;
            }

            await ApplyGatewayResultAsync(gateway, callback);

            return callback.Acknowledgement;
        }

        private async Task ApplyGatewayResultAsync(IPaymentGateway gateway, GatewayCallback callback)
        {
            // Valid event that changes nothing (e.g. "in process").
            if (callback.Status == PaymentStatus.Pending)
            {
                return;
            }

            Payment? payment;

            if (callback.PaymentId.HasValue)
            {
                payment = await _unitOfWork.Payments.GetByIdAsync(callback.PaymentId.Value);
            }
            else if (callback.IsTestEvent && _options.Development.MapUnreferencedTestEvents)
            {
                // DEVELOPMENT ONLY (the option is refused outside Development, and only TEST events get here).
                // "stripe trigger" produces events that know nothing about our payments, so they are
                // mapped to the latest payment that has a pending checkout with this provider.
                payment = await _unitOfWork.Payments.GetLatestPendingByProviderAsync(gateway.Provider);

                if (payment != null)
                {
                    _logger.LogWarning(
                        "DEV: {Provider} test event without a payment reference was mapped to pending payment {PaymentId}.",
                        gateway.ProviderName,
                        payment.Id);
                }
            }
            else
            {
                // Valid signature but nothing that points at one of our payments: acknowledge and move on,
                // an error response would only make the provider retry the same event.
                _logger.LogWarning(
                    "{Provider} webhook carries no payment reference and was ignored.",
                    gateway.ProviderName);
                return;
            }

            if (payment == null)
            {
                // The signature is valid, so retrying will not help. Log it and stop the retries.
                _logger.LogWarning(
                    "{Provider} webhook refers to an unknown payment (reference: {PaymentId}).",
                    gateway.ProviderName,
                    callback.PaymentId?.ToString() ?? "none, and no pending checkout to map to");
                return;
            }

            if (callback.Status == PaymentStatus.Completed)
            {
                var skipAmountCheck = callback.IsTestEvent && _options.Development.SkipAmountCheckForTestEvents;

                if (skipAmountCheck)
                {
                    _logger.LogWarning(
                        "DEV: amount check skipped for {Provider} test event on payment {PaymentId} (event: {Amount} {Currency}, payment: {ExpectedAmount} {ExpectedCurrency}).",
                        gateway.ProviderName,
                        payment.Id,
                        callback.Amount,
                        callback.Currency,
                        payment.Amount,
                        payment.Currency);
                }
                // The provider must have charged exactly what we asked for.
                else if (callback.Amount != payment.Amount ||
                    !string.Equals(callback.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(
                        "{Provider} webhook for payment {PaymentId}: expected {ExpectedAmount} {ExpectedCurrency}, got {Amount} {Currency}.",
                        gateway.ProviderName,
                        payment.Id,
                        payment.Amount,
                        payment.Currency,
                        callback.Amount,
                        callback.Currency);

                    throw new ConflictException("Paid amount or currency does not match the payment.");
                }
            }

            // Each transition is an atomic compare-and-set, so a duplicate or concurrent
            // webhook can never apply the same change (or send the notifications) twice.
            var paidAt = callback.Status == PaymentStatus.Completed ? DateTime.UtcNow : (DateTime?)null;
            bool changed;

            switch (callback.Status)
            {
                case PaymentStatus.Completed:
                    // A late success after a failed attempt is still a success.
                    changed =
                        await _unitOfWork.Payments.TryUpdateStatusAsync(
                            payment.Id, PaymentStatus.Pending, PaymentStatus.Completed, paidAt, callback.ExternalTransactionId)
                        ||
                        await _unitOfWork.Payments.TryUpdateStatusAsync(
                            payment.Id, PaymentStatus.Failed, PaymentStatus.Completed, paidAt, callback.ExternalTransactionId);
                    break;

                case PaymentStatus.Failed:
                    changed = await _unitOfWork.Payments.TryUpdateStatusAsync(
                        payment.Id, PaymentStatus.Pending, PaymentStatus.Failed, null, callback.ExternalTransactionId);
                    break;

                case PaymentStatus.Refunded:
                    changed = await _unitOfWork.Payments.TryUpdateStatusAsync(
                        payment.Id, PaymentStatus.Completed, PaymentStatus.Refunded, null, callback.ExternalTransactionId);
                    break;

                default:
                    changed = false;
                    break;
            }

            if (!changed)
            {
                _logger.LogInformation(
                    "{Provider} webhook for payment {PaymentId} ignored: {Status} is not applicable to the current state.",
                    gateway.ProviderName,
                    payment.Id,
                    callback.Status);
                return;
            }

            _logger.LogInformation(
                "{Provider}: payment {PaymentId} is now {Status}.",
                gateway.ProviderName,
                payment.Id,
                callback.Status);

            await NotifyAboutResultAsync(payment, callback.Status);
        }

        // The money state is already committed; a failing notification must not turn the
        // webhook into an error (the provider would just keep retrying).
        private async Task NotifyAboutResultAsync(Payment payment, PaymentStatus status)
        {
            try
            {
                switch (status)
                {
                    case PaymentStatus.Completed:
                        await _notificationService.CreateNotificationAsync(
                            userId: payment.UserId,
                            title: "Payment successful",
                            message: $"Your payment of {payment.Amount} for order #{payment.OrderId} was successfully processed.");

                        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
                        if (order != null && !string.IsNullOrEmpty(order.ExecutorId))
                        {
                            await _notificationService.CreateNotificationAsync(
                                userId: order.ExecutorId,
                                title: "Order paid",
                                message: $"The customer has paid for order #{order.Id}.");
                        }
                        break;

                    case PaymentStatus.Failed:
                        await _notificationService.CreateNotificationAsync(
                            userId: payment.UserId,
                            title: "Payment failed",
                            message: $"Payment for order #{payment.OrderId} could not be completed.");
                        break;

                    case PaymentStatus.Refunded:
                        await _notificationService.CreateNotificationAsync(
                            userId: payment.UserId,
                            title: "Payment refunded",
                            message: $"Your payment of {payment.Amount} for order #{payment.OrderId} has been refunded.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send notifications for payment {PaymentId}.", payment.Id);
            }
        }

        // ------------------------------------------------------------------ refunds

        // NOTE: behaviour intentionally unchanged (not part of K2/K3). It only flips the status
        // in the database and does NOT call the provider's refund API, so no money moves.
        // It should be restricted to admins and wired to the gateways before production use.
        public async Task RefundPaymentAsync(int paymentId, string currentUserId)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new NotFoundException($"Payment with ID {paymentId} does not exist.");
            }
            if (payment.UserId != currentUserId)
            {
                throw new ForbiddenException("You are not authorized to refund this payment.");
            }
            if (payment.Status != PaymentStatus.Completed)
            {
                throw new ConflictException("Only completed payments can be refunded.");
            }

            await _unitOfWork.Payments.UpdateStatusAsync(payment.Id, PaymentStatus.Refunded);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                userId: payment.UserId,
                title: "Payment refunded",
                message: $"Your payment of {payment.Amount} for order #{payment.OrderId} has been refunded.");
        }

        // ------------------------------------------------------------------ helpers

        // Payments of an order are visible to the customer, the assigned executor and admins only.
        private async Task EnsureCanViewOrderAsync(int orderId, string userId, bool isAdmin)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException($"Order with ID {orderId} does not exist.");
            }

            if (!isAdmin && order.CustomerId != userId && order.ExecutorId != userId)
            {
                throw new ForbiddenException("You do not have access to the payments of this order.");
            }
        }
    }
}