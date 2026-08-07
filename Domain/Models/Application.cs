using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Models
{
    public class Application
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string? ExecutorId { get; set; }
        public ApplicationUser? Executor { get; set; }

        public decimal ProposedPrice { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }

    }
}
