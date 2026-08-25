namespace API.DTO.Review
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? OrderTitle { get; set; }

        public string AuthorId { get; set; } = string.Empty;
        public string? AuthorName { get; set; }

        public string TargetUserId { get; set; } = string.Empty;
        public string? TargetUserName { get; set; }

        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}