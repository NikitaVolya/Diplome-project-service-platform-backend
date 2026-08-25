using BLL.Services.Interfaces;
using DAL.Repositories.Interfaces;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Payments.GetByIdAsync(id);
        }

        public async Task<Payment?> GetByExternalTransactionIdAsync(string externalTransactionId)
        {
            if (string.IsNullOrWhiteSpace(externalTransactionId))
            {
                throw new ArgumentException("External transaction ID cannot be null or empty.", nameof(externalTransactionId));
            }
            return await _unitOfWork.Payments.GetByExternalTransactionIdAsync(externalTransactionId);
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }
            return await _unitOfWork.Payments.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId)
        {
            return await _unitOfWork.Payments.GetByOrderIdAsync(orderId);
        }

        public async Task<bool> IsOrderPaidAsync(int orderId)
        {
            return await _unitOfWork.Payments.IsOrderPaidAsync(orderId);
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            if (payment.Amount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than zero.", nameof(payment.Amount));
            }

            if (string.IsNullOrWhiteSpace(payment.UserId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(payment.UserId));
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {payment.OrderId} does not exist.");
            }

            var isAlreadyPaid = await _unitOfWork.Payments.IsOrderPaidAsync(payment.OrderId);
            if (isAlreadyPaid)
            {
                throw new InvalidOperationException($"Order with ID {payment.OrderId} has already been paid.");
            }

            payment.CreatedAt = DateTime.UtcNow;
            payment.Status = PaymentStatus.Pending;
            payment.PaidAt = null;

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return payment;
        }

        public async Task ProcessCallbackAsync(string externalTransactionId, PaymentStatus newStatus)
        {
            var payment = await _unitOfWork.Payments.GetByExternalTransactionIdAsync(externalTransactionId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment with external transaction ID {externalTransactionId} does not exist.");
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                throw new InvalidOperationException($"Payment with external transaction ID {externalTransactionId} has already been completed.");
            }

            DateTime? paidAt = newStatus == PaymentStatus.Completed ? DateTime.UtcNow : null;

            await _unitOfWork.Payments.UpdateStatusAsync(payment.Id, newStatus, paidAt);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RefundPaymentAsync(int paymentId, string currentUserId)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment with ID {paymentId} does not exist.");
            }
            if (payment.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to refund this payment.");
            }
            if (payment.Status != PaymentStatus.Completed)
            {
                throw new InvalidOperationException("Only completed payments can be refunded.");
            }
            await _unitOfWork.Payments.UpdateStatusAsync(payment.Id, PaymentStatus.Refunded);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
