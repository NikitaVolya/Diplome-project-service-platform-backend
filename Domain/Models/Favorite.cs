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

        public int UserId { get; set; }

        public int? TargetOrderId { get; set; }
        public Order? TargetOrder { get; set; }

        public int? TargetExecutorId { get; set; }
        public string? TargetExecutorName { get; set; }
    }
}
