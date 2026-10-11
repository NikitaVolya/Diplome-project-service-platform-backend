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

        // ── Анкета з другого кроку реєстрації ───────────────────────────────

        /// <summary>Виконавець може виступати приватною особою або компанією чи ФОП.</summary>
        public bool IsCompany { get; set; }

        /// <summary>Назва компанії — тільки для тих, хто обрав «Компанія/ФОП».</summary>
        public string? CompanyName { get; set; }

        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public DateTime? BirthDate { get; set; }

        /// <summary>«Male», «Female» або null, якщо людина не захотіла вказувати.</summary>
        public string? Gender { get; set; }

        /// <summary>Досвід роботи виконавця: ключ зі списку на формі, не вільний текст.</summary>
        public string? Experience { get; set; }

        /// <summary>
        /// Напрям каталогу, у якому працює виконавець.
        ///
        /// Навмисно без зовнішнього ключа: напрями не видаляються, а ховаються, і вибір
        /// людини не повинен заважати адміністраторові прибрати напрям із вітрини.
        /// </summary>
        public int? SpecializationCategoryId { get; set; }

        /// <summary>Кілька слів про себе з форми реєстрації.</summary>
        public string? About { get; set; }

        public bool NotifyByEmail { get; set; }
        public bool NotifyBySms { get; set; }
        public bool NotifyByPush { get; set; }

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
