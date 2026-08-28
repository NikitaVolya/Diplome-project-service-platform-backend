using Domain.Models;

namespace BLL.Admin.Models
{
    /// <summary>
    /// Поля, спільні для всіх списків адмінки. Прив'язуються прямо з рядка запиту,
    /// тому кожна властивість має нормально працювати й тоді, коли значення немає.
    /// </summary>
    public abstract class FilterBase
    {
        private int _pageSize = 20;

        public int Page { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value is > 0 and <= 200 ? value : 20;
        }

        /// <summary>Довільний текстовий пошук; що саме вважається збігом, визначає кожен список окремо.</summary>
        public string? Search { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public string? SortBy { get; set; }

        public bool SortDesc { get; set; } = true;

        /// <summary>Включна верхня межа: «до 2026-08-28» має охоплювати весь той день.</summary>
        public DateTime? ToInclusive => To?.Date.AddDays(1);

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        public string NormalizedSearch => (Search ?? string.Empty).Trim();
    }

    public class UserFilter : FilterBase
    {
        /// <summary>Назва ролі Identity для фільтрації, наприклад «Executor».</summary>
        public string? Role { get; set; }

        public UserState State { get; set; } = UserState.Any;
    }

    public enum UserState
    {
        Any,
        Active,
        Blocked,
        Deleted
    }

    public class OrderFilter : FilterBase
    {
        public OrderStatus? Status { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }
    }

    public class ComplaintFilter : FilterBase
    {
        public ComplaintStatus? Status { get; set; }
    }

    public class ReviewFilter : FilterBase
    {
        public int? MinRating { get; set; }

        public int? MaxRating { get; set; }
    }

    public class PaymentFilter : FilterBase
    {
        public PaymentStatus? Status { get; set; }

        public PaymentProvider? Provider { get; set; }
    }

    public class AuditLogFilter : FilterBase
    {
        public AuditAction? Action { get; set; }

        public AuditSeverity? Severity { get; set; }

        public string? EntityName { get; set; }
    }

    public class CategoryFilter : FilterBase
    {
        public bool? IsActive { get; set; }
    }
}
