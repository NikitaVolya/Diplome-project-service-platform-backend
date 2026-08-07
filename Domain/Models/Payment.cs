using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "UAH";

        public PaymentProvider Provider { get; set; }
        public string ExternalTransactionId { get; set; } = string.Empty;
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
    public enum PaymentProvider
    {
        Stripe,
        LiqPay,
        WayForPay
    }
}
