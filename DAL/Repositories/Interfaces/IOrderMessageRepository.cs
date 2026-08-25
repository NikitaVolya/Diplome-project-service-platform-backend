using Domain.Entities;
using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IOrderMessageRepository : IGenericRepository<OrderMessage>
    {
        Task<IEnumerable<OrderMessage>> GetMessagesByOrderIdAsync(int orderId);

        Task<IEnumerable<Order>> GetOrdersWithMessagesForUserAsync(string userId);

        Task MarkMessagesAsReadAsync(int orderId, string currentUserId);

        Task<int> GetTotalUnreadCountAsync(string userId);
    }
}