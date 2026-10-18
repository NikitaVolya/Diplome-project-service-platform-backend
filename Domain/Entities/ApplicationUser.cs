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
        public bool IsCompany { get; set; }
        public string? CompanyName { get; set; }

        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Experience { get; set; }
        public int? SpecializationCategoryId { get; set; }
        public string? About { get; set; }

        public bool NotifyByEmail { get; set; }
        public bool NotifyBySms { get; set; }
        public bool NotifyByPush { get; set; }
        public bool NotifyMarketing { get; set; }
        public bool NotifyOrders { get; set; } = true;
        public bool NotifyMessages { get; set; } = true;
        public bool NotifyReviews { get; set; } = true;
        public bool NotifyReminders { get; set; } = true;
        public decimal WalletBalance { get; set; }
        public string? ProfileVisibility { get; set; }
        public bool ShowOnlineStatus { get; set; } = true;
        public bool ShareOrderHistory { get; set; }
        public string? Language { get; set; }
        public string? TimeZoneName { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? Theme { get; set; }
        public string? PaymentCard { get; set; }
        public string? Currency { get; set; }
        public string? Headline { get; set; }
        public string? Nickname { get; set; }
        public int ProfileViews { get; set; }
        public string? Presence { get; set; }

        public ICollection<ExecutorService> Services { get; set; } = new List<ExecutorService>();
        public ICollection<PortfolioWork> Portfolio { get; set; } = new List<PortfolioWork>();
        public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
        public ICollection<ProfileVisit> Visits { get; set; } = new List<ProfileVisit>();
        public ICollection<PayoutMethod> PayoutMethods { get; set; } = new List<PayoutMethod>();
        public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
        public ICollection<SupportTicket> Tickets { get; set; } = new List<SupportTicket>();

        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();

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
