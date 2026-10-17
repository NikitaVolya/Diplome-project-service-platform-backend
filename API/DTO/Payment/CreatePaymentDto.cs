using System.ComponentModel.DataAnnotations;

namespace API.DTO.Payment
{
    public class CreatePaymentDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? ExternalTransactionId { get; set; }
    }
}