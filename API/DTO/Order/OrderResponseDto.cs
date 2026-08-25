using Domain.Models;

namespace API.DTO.Order
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }
    }
}
