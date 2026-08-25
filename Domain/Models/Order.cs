using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExecutionAt { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = null!;

        public string? ExecutorId { get; set; }
        public ApplicationUser? Executor { get; set; }

        public ICollection<Application> Applications { get; set; } = new List<Application>();

        public ICollection<OrderMessage> OrderMessages { get; set; } = new List<OrderMessage>();
    }

    public enum OrderStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}
