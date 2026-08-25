using Domain.Models;

namespace API.DTO.Application
{
    public class ApplicationResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? OrderTitle { get; set; }

        public string ExecutorId { get; set; } = string.Empty;
        public string? ExecutorName { get; set; }

        public decimal ProposedPrice { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public ApplicationStatus Status { get; set; }
    }
}
