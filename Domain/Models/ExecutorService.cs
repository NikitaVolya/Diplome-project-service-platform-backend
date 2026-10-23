using Domain.Entities;

namespace Domain.Models
{
    public class ExecutorService
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string? Description { get; set; }

        public decimal FromPrice { get; set; }

        public ServiceState State { get; set; } = ServiceState.Draft;

        public string? Image { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum ServiceState
    {
        Draft,

        Active,

        Paused
    }
}
