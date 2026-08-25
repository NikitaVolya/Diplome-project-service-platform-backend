using System.ComponentModel.DataAnnotations;

namespace API.DTO.Notification
{
    public class CreateMassNotificationDto
    {
        [Required]
        public List<string> UserIds { get; set; } = new();

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
    }
}