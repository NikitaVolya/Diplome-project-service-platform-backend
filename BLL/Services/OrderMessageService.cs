using DAL.UnitOfWork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;
using BLL.Services.Interfaces;

namespace BLL.Services
{
    public class OrderMessageService : IOrderMessageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderMessageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OrderMessage> SendMessageAsync(int orderId, string senderId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text cannot be empty.", nameof(text));
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException("Order not found.", nameof(orderId));
            }

            if (order.CustomerId != senderId && order.ExecutorId != senderId)
            {
                throw new UnauthorizedAccessException("Sender is not authorized to send messages for this order.");
            }

            var message = new OrderMessage
            {
                OrderId = orderId,
                SenderId = senderId,
                Text = text,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.OrderMessages.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return message;
        }

        public async Task<IEnumerable<OrderMessage>> GetOrderMessagesAsync(int orderId, string currentUuserId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException("Order not found.", nameof(orderId));
            }

            if (order.CustomerId != currentUuserId && order.ExecutorId != currentUuserId)
            {
                throw new UnauthorizedAccessException("User is not authorized to view messages for this order.");
            }

            await _unitOfWork.OrderMessages.MarkMessagesAsReadAsync(orderId, currentUuserId);

            var messages = await _unitOfWork.OrderMessages.GetMessagesByOrderIdAsync(orderId);
            return messages;
        }

        public async Task<IEnumerable<Order>> GetUserDialogsAsync(string currentUserId)
        {
            var ordersWithMessages = await _unitOfWork.OrderMessages.GetOrdersWithMessagesForUserAsync(currentUserId);
            return ordersWithMessages.OrderByDescending(o => o.OrderMessages.Max(m => m.SentAt)).ToList();
        }

        public async Task<int> GetTotalUnreadCountAsync(string currentUserId)
        {
            var unreadCount = await _unitOfWork.OrderMessages.GetTotalUnreadCountAsync(currentUserId);
            return unreadCount;
        }
    }
}
