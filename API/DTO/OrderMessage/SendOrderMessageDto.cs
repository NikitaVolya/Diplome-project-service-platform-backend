using System.ComponentModel.DataAnnotations;

namespace API.DTO.OrderMessage
{
    public class SendOrderMessageDto
    {
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Text { get; set; } = string.Empty;
    }
}