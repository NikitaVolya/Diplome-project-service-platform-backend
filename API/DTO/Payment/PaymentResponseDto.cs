using Domain.Models;

namespace API.DTO.Payment
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? OrderTitle { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? ExternalTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}