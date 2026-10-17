using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class OrderMessageRepository : GenericRepository<OrderMessage>, IOrderMessageRepository
    {
        public OrderMessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<OrderMessage>> GetConversationAsync(int orderId, string userAId, string userBId)
        {
            return await _context.OrderMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.OrderId == orderId &&
                            ((m.SenderId == userAId && m.ReceiverId == userBId) ||
                             (m.SenderId == userBId && m.ReceiverId == userAId)))
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<bool> HasConversationAsync(int orderId, string userAId, string userBId)
        {
            return await _context.OrderMessages
                .AnyAsync(m => m.OrderId == orderId &&
                               ((m.SenderId == userAId && m.ReceiverId == userBId) ||
                                (m.SenderId == userBId && m.ReceiverId == userAId)));
        }

        public async Task MarkConversationAsReadAsync(int orderId, string senderId, string receiverId)
        {
            await _context.OrderMessages
                .Where(m => m.OrderId == orderId &&
                            m.SenderId == senderId &&
                            m.ReceiverId == receiverId &&
                            !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        }

        public async Task<IReadOnlyList<(OrderMessage LastMessage, int UnreadCount)>> GetDialogSummariesAsync(string userId, int? orderId = null)
        {
            // A conversation is identified by (order, the OTHER participant). Grouping happens in SQL:
            // Id grows with every insert, so the maximum Id is the newest message of the conversation.
            var groups = await _context.OrderMessages
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                            (orderId == null || m.OrderId == orderId))
                .GroupBy(m => new
                {
                    m.OrderId,
                    CounterpartId = m.SenderId == userId ? m.ReceiverId : m.SenderId
                })
                .Select(g => new
                {
                    LastMessageId = g.Max(m => m.Id),
                    UnreadCount = g.Count(m => !m.IsRead && m.ReceiverId == userId)
                })
                .ToListAsync();

            if (groups.Count == 0)
            {
                return Array.Empty<(OrderMessage LastMessage, int UnreadCount)>();
            }

            var lastMessageIds = groups.Select(g => g.LastMessageId).ToList();

            var lastMessages = await _context.OrderMessages
                .AsNoTracking()
                .Include(m => m.Order)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => lastMessageIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            return groups
                .Select(g => (lastMessages[g.LastMessageId], g.UnreadCount))
                .ToList();
        }

        public async Task<int> GetTotalUnreadCountAsync(string userId)
        {
            return await _context.OrderMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        }
    }
}
