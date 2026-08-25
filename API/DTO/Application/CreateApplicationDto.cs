using System.ComponentModel.DataAnnotations;

namespace API.DTO.Application
{
    public class CreateApplicationDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than zero.")]
        public decimal ProposedPrice { get; set; }

        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }
    }
}
