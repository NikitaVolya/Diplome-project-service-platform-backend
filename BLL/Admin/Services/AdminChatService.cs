using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    // Бік підтримки в чаті замовлення. Адміністратор бачить усі діалоги (на відміну від мобільного клієнта, де доступні лише замовник і виконавець) і може підключитися до будь-якого з них.
    public class AdminChatService : IAdminChatService
    {
        private const int MaxMessageLength = 1000;

        private readonly ApplicationDbContext _db;

        public AdminChatService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<AdminDialogListItem>> GetDialogsAsync(
            string? search,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderMessages.Any());

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    o.Title.Contains(term) ||
                    o.Customer.FirstName.Contains(term) ||
                    o.Customer.LastName.Contains(term) ||
                    (o.Executor != null && (o.Executor.FirstName.Contains(term) || o.Executor.LastName.Contains(term))));
            }

            return await query
                .Select(o => new AdminDialogListItem
                {
                    OrderId = o.Id,
                    OrderTitle = o.Title,
                    OrderStatus = o.Status,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    ExecutorName = o.Executor == null ? null : o.Executor.FirstName + " " + o.Executor.LastName,
                    MessagesCount = o.OrderMessages.Count,
                    UnreadCount = o.OrderMessages.Count(m => !m.IsRead),
                    LastMessage = o.OrderMessages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Text)
                        .FirstOrDefault(),
                    LastMessageAt = o.OrderMessages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => (DateTime?)m.SentAt)
                        .FirstOrDefault()
                })
                .OrderByDescending(d => d.LastMessageAt)
                .Take(200)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AdminChatMessage>> GetMessagesAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            return await _db.OrderMessages
                .AsNoTracking()
                .Where(m => m.OrderId == orderId)
                .OrderBy(m => m.SentAt)
                .Select(m => new AdminChatMessage
                {
                    Id = m.Id,
                    OrderId = m.OrderId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                    Text = m.Text,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminChatMessage> SendAsync(
            int orderId,
            string senderId,
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text cannot be empty.", nameof(text));
            }

            var orderExists = await _db.Orders.AnyAsync(o => o.Id == orderId, cancellationToken);
            if (!orderExists)
            {
                throw new InvalidOperationException($"Order #{orderId} does not exist.");
            }

            var trimmed = text.Trim();
            if (trimmed.Length > MaxMessageLength)
            {
                trimmed = trimmed[..MaxMessageLength];
            }

            var message = new OrderMessage
            {
                OrderId = orderId,
                SenderId = senderId,
                Text = trimmed,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.OrderMessages.Add(message);
            await _db.SaveChangesAsync(cancellationToken);

            var sender = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == senderId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(cancellationToken);

            return new AdminChatMessage
            {
                Id = message.Id,
                OrderId = message.OrderId,
                SenderId = message.SenderId,
                SenderName = string.IsNullOrWhiteSpace(sender) ? "Support" : sender!,
                Text = message.Text,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };
        }

        public async Task MarkReadAsync(int orderId, string readerId, CancellationToken cancellationToken = default)
        {
            var unread = await _db.OrderMessages
                .Where(m => m.OrderId == orderId && !m.IsRead && m.SenderId != readerId)
                .ToListAsync(cancellationToken);

            if (unread.Count == 0)
            {
                return;
            }

            foreach (var message in unread)
            {
                message.IsRead = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
