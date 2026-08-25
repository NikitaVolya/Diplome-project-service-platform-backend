using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
        Task<IEnumerable<Notification>> GetUnreadUserNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);

        Task<Notification> CreateNotificationAsync(string userId, string title, string message);
        Task SendMassNotificationAsync(IEnumerable<string> userIds, string title, string message);

        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId, string userId);
    }
}