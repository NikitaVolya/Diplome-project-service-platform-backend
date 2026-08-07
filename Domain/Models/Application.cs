using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Application
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }

        public decimal ProposedPrice { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }

    }
}
