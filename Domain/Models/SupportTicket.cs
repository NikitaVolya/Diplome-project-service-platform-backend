using Domain.Entities;

namespace Domain.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public string Topic { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.New;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AnsweredAt { get; set; }
    }

    public enum TicketStatus
    {
        New,

        InWork,

        Answered,

        Closed
    }
}
