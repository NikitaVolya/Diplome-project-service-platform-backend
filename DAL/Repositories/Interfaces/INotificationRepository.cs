using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId);

        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId);

        Task<int> GetUnreadCountByUserIdAsync(string userId);

        Task MarkAllAsReadAsync(string userId);
    }
}