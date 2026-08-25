using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace API.DTO.Payment
{
    public class PaymentCallbackDto
    {
        [Required]
        public string ExternalTransactionId { get; set; } = string.Empty;

        [Required]
        public PaymentStatus NewStatus { get; set; }
    }
}