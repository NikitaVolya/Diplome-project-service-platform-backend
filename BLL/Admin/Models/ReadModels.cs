using Domain.Models;

namespace BLL.Admin.Models
{
    // ---------------------------------------------------------------------
    // Користувачі
    // ---------------------------------------------------------------------

    public class AdminUserListItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int OrdersAsCustomer { get; set; }
        public int OrdersAsExecutor { get; set; }
        public double? AverageRating { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

        public bool IsBlocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

        public string StatusLabel =>
            IsDeleted ? "Deleted" : IsBlocked ? "Blocked" : "Active";
    }

    public class AdminUserDetails : AdminUserListItem
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalEarned { get; set; }
        public int ReviewsWritten { get; set; }
        public int ReviewsReceived { get; set; }
        public int ComplaintsAgainst { get; set; }
        public IReadOnlyList<AdminOrderListItem> RecentOrders { get; set; } = Array.Empty<AdminOrderListItem>();
        public IReadOnlyList<AdminReviewListItem> RecentReviews { get; set; } = Array.Empty<AdminReviewListItem>();
        public IReadOnlyList<AdminComplaintListItem> RecentComplaints { get; set; } = Array.Empty<AdminComplaintListItem>();
    }

    // ---------------------------------------------------------------------
    // Замовлення
    // ---------------------------------------------------------------------

    public class AdminOrderListItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public OrderStatus Status { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutionAt { get; set; }
        public int ApplicationsCount { get; set; }
    }

    public class AdminOrderDetails : AdminOrderListItem
    {
        public string? Description { get; set; }
        public IReadOnlyList<AdminApplicationListItem> Applications { get; set; } = Array.Empty<AdminApplicationListItem>();
        public IReadOnlyList<AdminPaymentListItem> Payments { get; set; } = Array.Empty<AdminPaymentListItem>();
        public IReadOnlyList<AdminReviewListItem> Reviews { get; set; } = Array.Empty<AdminReviewListItem>();
        public int MessagesCount { get; set; }
    }

    public class AdminApplicationListItem
    {
        public int Id { get; set; }
        public string? ExecutorId { get; set; }
        public string ExecutorName { get; set; } = string.Empty;
        public decimal ProposedPrice { get; set; }
        public string? Comment { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------------------------------------------------------------------
    // Категорії
    // ---------------------------------------------------------------------

    public class AdminCategoryListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public int SubCategoriesCount { get; set; }
        public int OrdersCount { get; set; }

        /// <summary>0 для кореневої категорії, 1 для підкатегорії; потрібно лише для відступу в дереві.</summary>
        public int Depth { get; set; }
    }

    public class AdminCategoryEditModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ---------------------------------------------------------------------
    // Відгуки / скарги / платежі
    // ---------------------------------------------------------------------

    public class AdminReviewListItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetUserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminComplaintListItem
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ComplaintStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Скільки всього скарг подано на цього самого користувача.</summary>
        public int TargetComplaintCount { get; set; }
    }

    public class AdminPaymentListItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "UAH";
        public PaymentProvider Provider { get; set; }
        public PaymentStatus Status { get; set; }
        public string ExternalTransactionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    // ---------------------------------------------------------------------
    // Журнал дій
    // ---------------------------------------------------------------------

    public class AdminAuditLogListItem
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public AuditAction Action { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public AuditSeverity Severity { get; set; }
    }

    // ---------------------------------------------------------------------
    // Чат
    // ---------------------------------------------------------------------

    public class AdminDialogListItem
    {
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;
        public OrderStatus OrderStatus { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? ExecutorName { get; set; }
        public int MessagesCount { get; set; }
        public int UnreadCount { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }

    public class AdminChatMessage
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}
