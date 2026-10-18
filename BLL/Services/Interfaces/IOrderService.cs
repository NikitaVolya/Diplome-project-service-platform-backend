using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetWithDetailsByIdAsync(int id);
        Task<IEnumerable<Order>> GetCustomerOrdersAsync(string customerId);
        Task<IEnumerable<Order>> GetExecutorOrdersAsync(string executorId);
        Task<(IEnumerable<Order> Items, int TotalCount)> GetFilteredOrdersAsync(
            int? categoryId,
            OrderStatus? status,
            string? searchTerm,
            double? latitude = null,
            double? longitude = null,
            double? radiusKm = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task<Order> CreateOrderAsync(Order order);
        Task UpdateOrderAsync(Order order, string currentUserId);
        Task CancelOrderAsync(int orderId, string currentUserId);
        Task CompleteOrderAsync(int orderId, string currentUserId);
        Task AssignExecutorAsync(int orderId, string executorId, string currentUserId);
        Task DeleteOrderAsync(int orderId, string currentUserId);
    }
}