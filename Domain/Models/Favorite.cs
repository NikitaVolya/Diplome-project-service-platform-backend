using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int? TargetOrderId { get; set; }
        public Order? TargetOrder { get; set; }

        public string? TargetExecutorId { get; set; }
        public ApplicationUser? TargetExecutor { get; set; }
    }
}
