using Domain.Models;


namespace API.DTO.Payment
{
    public class PaymentCallbackDto
    {
        public string ExternalTransactionId { get; set; } = string.Empty;
        public PaymentStatus? NewStatus { get; set; }
    }
}