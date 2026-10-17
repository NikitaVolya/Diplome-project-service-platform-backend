using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetWithDetailsByIdAsync(int id);

        Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId);

        Task<IEnumerable<Order>> GetByExecutorIdAsync(string executorId);

        Task<(IEnumerable<Order> Items, int TotalCount)> GetFilteredOrdersAsync(
            int? categoryId,
            OrderStatus? status,
            string? searchTerm,
            double? latitude = null,
            double? longitude = null,
            double? radiusKm = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task UpdateStatusAsync(int orderId, OrderStatus newStatus);
    }
}