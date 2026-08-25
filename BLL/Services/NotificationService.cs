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
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            return await _unitOfWork.Notifications.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Notification>> GetUnreadUserNotificationsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }
            return await _unitOfWork.Notifications.GetUnreadByUserIdAsync(userId);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }
            return await _unitOfWork.Notifications.GetUnreadCountByUserIdAsync(userId);
        }

        public async Task<Notification> CreateNotificationAsync(string userId, string title, string message)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Title cannot be null or empty.", nameof(title));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            return notification;
        }

        public async Task SendMassNotificationAsync(IEnumerable<string> userIds, string title, string message)
        {
            if (userIds == null || !userIds.Any())
                return;

            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            foreach (var notification in notifications)
            {
                await _unitOfWork.Notifications.AddAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null)
            {
                throw new InvalidOperationException("Notification not found.");
            }

            if (notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to mark this notification as read.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _unitOfWork.Notifications.UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _unitOfWork.Notifications.GetUnreadByUserIdAsync(userId);
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                await _unitOfWork.Notifications.UpdateAsync(notification);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null)
            {
                throw new InvalidOperationException("Notification not found.");
            }
            if (notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this notification.");
            }
            await _unitOfWork.Notifications.DeleteAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
