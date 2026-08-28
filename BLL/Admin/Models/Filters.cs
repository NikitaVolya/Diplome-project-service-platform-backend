using Domain.Models;

namespace BLL.Admin.Models
{
    /// <summary>
    /// Fields shared by every admin list screen. Bound straight from the query string,
    /// so every property must tolerate being absent.
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

        /// <summary>Free-text query; the meaning of "matches" is defined per list.</summary>
        public string? Search { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public string? SortBy { get; set; }

        public bool SortDesc { get; set; } = true;

        /// <summary>Inclusive upper bound: "to 2026-08-28" must include everything that day.</summary>
        public DateTime? ToInclusive => To?.Date.AddDays(1);

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        public string NormalizedSearch => (Search ?? string.Empty).Trim();
    }

    public class UserFilter : FilterBase
    {
        /// <summary>Identity role name to filter by, e.g. "Executor".</summary>
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
