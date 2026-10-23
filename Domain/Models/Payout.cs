using Domain.Entities;

namespace Domain.Models
{
    public class Payout
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public decimal Amount { get; set; }

        public int? MethodId { get; set; }
        public PayoutMethod? Method { get; set; }

        public string? MethodLabel { get; set; }

        public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }

    public enum PayoutStatus
    {
        Pending,

        Paid,

        Rejected
    }
}
