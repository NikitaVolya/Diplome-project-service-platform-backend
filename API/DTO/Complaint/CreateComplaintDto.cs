using System.ComponentModel.DataAnnotations;

namespace API.DTO.Complaint
{
    public class CreateComplaintDto
    {
        public string? TargetUserId { get; set; }

        public int? OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}