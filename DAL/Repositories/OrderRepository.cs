using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetWithDetailsByIdAsync(int id)
        {
            return await _dbSet
                .Include(o => o.Category)
                .Include(o => o.Customer)
                .Include(o => o.Executor)
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Executor)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId)
        {
            return await _dbSet
                .Include(o => o.Category)
                .Include(o => o.Executor)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByExecutorIdAsync(string executorId)
        {
            return await _dbSet
                .Include(o => o.Category)
                .Include(o => o.Customer)
                .Where(o => o.ExecutorId == executorId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetFilteredOrdersAsync(
            int? categoryId,
            OrderStatus? status,
            string? searchTerm,
            double? latitude = null,
            double? longitude = null,
            double? radiusKm = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var query = _dbSet
                .Include(o => o.Category)
                .Include(o => o.Customer)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(o => o.CategoryId == categoryId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.Title.ToLower().Contains(term)
                                      || o.Description.ToLower().Contains(term)
                                      || (o.Address != null && o.Address.ToLower().Contains(term)));
            }

            if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
            {
                var lat = latitude.Value;
                var lng = longitude.Value;
                var r = radiusKm.Value;

                var latRange = r / 111.0;
                var lngRange = r / (111.0 * Math.Cos(lat * Math.PI / 180.0));

                query = query.Where(o =>
                    o.Latitude >= lat - latRange && o.Latitude <= lat + latRange &&
                    o.Longitude >= lng - lngRange && o.Longitude <= lng + lngRange);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task UpdateStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _dbSet.FindAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
            }
        }
    }
}