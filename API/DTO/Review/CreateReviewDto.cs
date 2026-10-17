using System.ComponentModel.DataAnnotations;

namespace API.DTO.Review
{
    public class CreateReviewDto
    {
        public int OrderId { get; set; }
        public string TargetUserId { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}