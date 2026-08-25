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
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Orders.GetByIdAsync(id);
        }

        public async Task<Order?> GetWithDetailsByIdAsync(int id)
        {
            return await _unitOfWork.Orders.GetWithDetailsByIdAsync(id);
        }

        public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(string customerId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                throw new ArgumentException("Customer ID cannot be null or empty.", nameof(customerId));
            }

            return await _unitOfWork.Orders.GetByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<Order>> GetExecutorOrdersAsync(string executorId)
        {
            if (string.IsNullOrEmpty(executorId))
            {
                throw new ArgumentException("Executor ID cannot be null or empty.", nameof(executorId));
            }
            return await _unitOfWork.Orders.GetByExecutorIdAsync(executorId);
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetFilteredOrdersAsync(
            int? categoryId,
            OrderStatus? status,
            string? searchTerm,
            int pageIndex = 1,
            int pageSize = 10)
        {
            return await _unitOfWork.Orders.GetFilteredOrdersAsync(categoryId, status, searchTerm, pageIndex, pageSize);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.Title))
            {
                throw new ArgumentException("Order title cannot be null or empty.", nameof(order.Title));
            }

            if (string.IsNullOrWhiteSpace(order.Description))
            {
                throw new ArgumentException("Order description cannot be null or empty.", nameof(order.Description));
            }

            if (order.Price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(order.Price), "Order price cannot be negative.");
            }

            var category = await _unitOfWork.Categories.GetByIdAsync(order.CategoryId);
            if (category == null)
            {
                throw new ArgumentException($"Category with ID {order.CategoryId} does not exist.", nameof(order.CategoryId));
            }

            order.CreatedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Pending;
            order.ExecutorId = null;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return order;
        }

        public async Task UpdateOrderAsync(Order order, string currentUserId)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(order.Id);
            if (existingOrder == null)
            {
                throw new ArgumentException($"Order with ID {order.Id} does not exist.", nameof(order.Id));
            }
            if (existingOrder.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this order.");
            }
            existingOrder.Title = order.Title;
            existingOrder.Description = order.Description;
            existingOrder.Price = order.Price;
            existingOrder.Status = order.Status;
            existingOrder.Address = order.Address;
            existingOrder.CategoryId = order.CategoryId;

            await _unitOfWork.Orders.UpdateAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(int orderId, string currentUserId)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                throw new ArgumentException($"Order with ID {orderId} does not exist.", nameof(orderId));
            }
            if (existingOrder.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to cancel this order.");
            }

            existingOrder.Status = OrderStatus.Cancelled;
            await _unitOfWork.Orders.UpdateAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteOrderAsync(int orderId, string currentUserId)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                throw new ArgumentException($"Order with ID {orderId} does not exist.", nameof(orderId));
            }
            if (existingOrder.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to complete this order.");
            }

            existingOrder.Status = OrderStatus.Completed;
            existingOrder.ExecutionAt = DateTime.UtcNow;
            await _unitOfWork.Orders.UpdateAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AssignExecutorAsync(int orderId, string executorId, string currentUserId)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                throw new ArgumentException($"Order with ID {orderId} does not exist.", nameof(orderId));
            }
            if (existingOrder.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to assign an executor to this order.");
            }

            if (existingOrder.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException("Executor can only be assigned to orders with Pending status.");
            }

            existingOrder.ExecutorId = executorId;
            existingOrder.Status = OrderStatus.InProgress;
            await _unitOfWork.Orders.UpdateAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteOrderAsync(int orderId, string currentUserId)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                throw new ArgumentException($"Order with ID {orderId} does not exist.", nameof(orderId));
            }
            if (existingOrder.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this order.");
            }
            await _unitOfWork.Orders.DeleteAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
