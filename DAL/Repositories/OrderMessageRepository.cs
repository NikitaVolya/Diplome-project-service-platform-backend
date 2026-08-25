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

        public async Task<IEnumerable<OrderMessage>> GetMessagesByOrderIdAsync(int orderId)
        {
            return await _context.OrderMessages
                .Include(m => m.Sender)
                .Where(m => m.OrderId == orderId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersWithMessagesForUserAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Executor)
                .Include(o => o.OrderMessages)
                    .ThenInclude(m => m.Sender)
                .Where(o => (o.CustomerId == userId || o.ExecutorId == userId) && o.OrderMessages.Any())
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(int orderId, string currentUserId)
        {
            await _context.OrderMessages
                .Where(m => m.OrderId == orderId && m.SenderId != currentUserId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        }

        public async Task<int> GetTotalUnreadCountAsync(string userId)
        {
            return await _context.OrderMessages
                .Where(m => m.SenderId != userId && !m.IsRead &&
                           (m.Order.CustomerId == userId || m.Order.ExecutorId == userId))
                .CountAsync();
        }
    }
}