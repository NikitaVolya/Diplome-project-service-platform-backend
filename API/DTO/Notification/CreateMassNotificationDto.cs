using System.ComponentModel.DataAnnotations;

namespace API.DTO.Notification
{
    public class CreateMassNotificationDto
    {
        public List<string> UserIds { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}