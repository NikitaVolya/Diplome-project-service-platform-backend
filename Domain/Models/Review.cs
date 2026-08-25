using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string AuthorId { get; set; } = string.Empty;
        public ApplicationUser Author { get; set; } = null!;

        public string TargetUserId { get; set; } = string.Empty;
        public ApplicationUser TargetUser { get; set; } = null!;

        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
