using BLL.Results;
using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IOrderMessageService
    {
        /// <summary>
        /// Sends a message inside ONE conversation (order + customer + executor).
        /// An executor always writes to the customer of the order; a customer must say which executor
        /// he writes to (receiverId), unless the order already has an assigned executor.
        /// </summary>
        Task<OrderMessage> SendMessageAsync(int orderId, string senderId, bool senderIsExecutor, string? receiverId, string text);

        /// <summary>
        /// Messages of one conversation. participantId is the other side of the conversation:
        /// required for a customer (unless the order has an assigned executor), optional for an executor.
        /// </summary>
        Task<IEnumerable<OrderMessage>> GetConversationAsync(int orderId, string currentUserId, bool currentUserIsExecutor, string? participantId);

        /// <summary>One entry per conversation of the user, newest first; optionally for a single order.</summary>
        Task<IEnumerable<OrderDialog>> GetUserDialogsAsync(string currentUserId, int? orderId = null);

        Task<int> GetTotalUnreadCountAsync(string currentUserId);
    }
}
