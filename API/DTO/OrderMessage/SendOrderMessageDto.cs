using System.ComponentModel.DataAnnotations;

namespace API.DTO.OrderMessage
{
    public class SendOrderMessageDto
    {
        // 1000 = the column length in the database (it used to be 2000, so long messages
        // passed validation and then failed on save).
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Id of the executor the CUSTOMER writes to. Required for a customer while the order has no
        /// assigned executor (then several executors may be talking to him at once); optional afterwards.
        /// Executors do not set it: they always write to the customer of the order.
        /// </summary>
        public string? ReceiverId { get; set; }
    }
}
