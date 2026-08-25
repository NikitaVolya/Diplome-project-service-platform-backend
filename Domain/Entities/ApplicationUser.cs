using Domain.Models;
using Microsoft.AspNetCore.Identity;


namespace Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
        public ICollection<Order> ExecutedOrders { get; set; } = new List<Order>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<Review> WrittenReviews { get; set; } = new List<Review>();
        public ICollection<Review> ReceivedReviews { get; set; } = new List<Review>();
        public ICollection<Complaint> SentComplaints { get; set; } = new List<Complaint>();
        public ICollection<Complaint> ReceivedComplaints { get; set; } = new List<Complaint>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public ICollection<OrderMessage> SentMessages { get; set; } = new List<OrderMessage>();
    }
}
