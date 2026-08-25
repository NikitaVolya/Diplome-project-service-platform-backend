using System.ComponentModel.DataAnnotations;

namespace API.DTO.Complaint
{
    public class CreateComplaintDto
    {
        public string? TargetUserId { get; set; }

        public int? OrderId { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(150, ErrorMessage = "Reason cannot exceed 150 characters.")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = string.Empty;
    }
}