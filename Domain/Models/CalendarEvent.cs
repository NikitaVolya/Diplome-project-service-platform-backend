using Domain.Entities;

namespace Domain.Models
{
    public class CalendarEvent
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public EventKind Kind { get; set; } = EventKind.Meeting;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public string? PartnerName { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum EventKind
    {
        Meeting,

        Deadline,

        Personal,

        Busy
    }
}
