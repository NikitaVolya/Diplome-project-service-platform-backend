using System.ComponentModel.DataAnnotations;

namespace API.DTO.Payment
{
    public class CreatePaymentDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public string? ExternalTransactionId { get; set; }
    }
}