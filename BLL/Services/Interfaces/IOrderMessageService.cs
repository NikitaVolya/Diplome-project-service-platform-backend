using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IOrderMessageService
    {
        Task<OrderMessage> SendMessageAsync(int orderId, string senderId, string text);

        Task<IEnumerable<OrderMessage>> GetOrderMessagesAsync(int orderId, string currentUserId);

        Task<IEnumerable<Order>> GetUserDialogsAsync(string currentUserId);

        Task<int> GetTotalUnreadCountAsync(string currentUserId);
    }
}