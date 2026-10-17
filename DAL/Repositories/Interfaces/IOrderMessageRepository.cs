using Domain.Entities;
using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IOrderMessageRepository : IGenericRepository<OrderMessage>
    {
        /// <summary>
        /// Messages of ONE conversation: the given order and exactly the two given users
        /// (in both directions), oldest first. Messages of the same order with other
        /// participants are never returned.
        /// </summary>
        Task<IEnumerable<OrderMessage>> GetConversationAsync(int orderId, string userAId, string userBId);

        /// <summary>True when at least one message was ever exchanged by these two users on this order.</summary>
        Task<bool> HasConversationAsync(int orderId, string userAId, string userBId);

        /// <summary>
        /// Marks as read the messages that <paramref name="senderId"/> sent to <paramref name="receiverId"/>
        /// on this order. Messages of other conversations are not touched.
        /// </summary>
        Task MarkConversationAsReadAsync(int orderId, string senderId, string receiverId);

        /// <summary>
        /// One entry per conversation (order + counterpart) of the user: the last message of that
        /// conversation and how many messages in it are unread by the user. Computed in SQL;
        /// the whole history is never loaded. Optionally limited to a single order.
        /// </summary>
        Task<IReadOnlyList<(OrderMessage LastMessage, int UnreadCount)>> GetDialogSummariesAsync(string userId, int? orderId = null);

        /// <summary>Messages addressed to the user that he has not read yet, over all conversations.</summary>
        Task<int> GetTotalUnreadCountAsync(string userId);
    }
}
