using Domain.Entities;

namespace Domain.Models
{
    public class PayoutMethod
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public PayoutKind Kind { get; set; } = PayoutKind.Card;

        public string Label { get; set; } = string.Empty;

        public string? Note { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
    }

    public enum PayoutKind
    {
        Card,

        PayPal,

        Iban
    }
}
