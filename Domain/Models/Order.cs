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
        public string Status { get; set; }

        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExecutionAt { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        public int? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
