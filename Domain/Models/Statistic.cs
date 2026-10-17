using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Statistic
    {
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewUsersCount { get; set; }
    }
}
