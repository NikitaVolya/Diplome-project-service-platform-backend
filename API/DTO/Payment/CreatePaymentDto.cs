using System.ComponentModel.DataAnnotations;

namespace API.DTO.Payment
{
    /// <summary>
    /// The client only says WHICH order it wants to pay for.
    /// The amount and currency are taken from the order on the server,
    /// and ExternalTransactionId is set by the payment provider integration, never by the client.
    /// </summary>
    public class CreatePaymentDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "OrderId must be a positive number.")]
        public int OrderId { get; set; }
    }
}
