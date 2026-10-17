using Domain.Models;

namespace API.DTO.Complaint
{
    public class ComplaintResponseDto
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = string.Empty;
        public string? SenderName { get; set; }

        public string? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ComplaintStatus Status { get; set; }
    }
}