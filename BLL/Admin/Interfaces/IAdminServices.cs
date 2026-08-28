using BLL.Admin.Models;
using Domain.Models;

namespace BLL.Admin.Interfaces
{
    /// <summary>Aggregated numbers and chart series for the dashboard and statistics screens.</summary>
    public interface IAdminDashboardService
    {
        Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

        /// <summary>Orders created vs. completed, one point per day.</summary>
        Task<ChartData> GetOrdersChartAsync(int days = 30, CancellationToken cancellationToken = default);

        /// <summary>Completed payment volume, one point per month.</summary>
        Task<ChartData> GetRevenueChartAsync(int months = 12, CancellationToken cancellationToken = default);

        /// <summary>Registrations, one point per day.</summary>
        Task<ChartData> GetUsersChartAsync(int days = 30, CancellationToken cancellationToken = default);

        /// <summary>Order counts grouped by status, for the doughnut chart.</summary>
        Task<ChartData> GetOrderStatusChartAsync(CancellationToken cancellationToken = default);

        /// <summary>Order counts for the busiest categories.</summary>
        Task<ChartData> GetCategoryChartAsync(int top = 8, CancellationToken cancellationToken = default);

        /// <summary>Review counts grouped by star rating (1..5).</summary>
        Task<ChartData> GetRatingChartAsync(CancellationToken cancellationToken = default);

        /// <summary>Daily rows for the statistics table, computed on the fly from orders and payments.</summary>
        Task<IReadOnlyList<Statistic>> GetDailyBreakdownAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);
    }

    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserListItem>> GetUsersAsync(UserFilter filter, CancellationToken cancellationToken = default);

        Task<AdminUserDetails?> GetUserAsync(string id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminUserListItem>> GetUsersForExportAsync(UserFilter filter, CancellationToken cancellationToken = default);

        /// <summary>Locks the account out until <paramref name="until"/>; null blocks it indefinitely.</summary>
        Task<AdminOperationResult> BlockAsync(string id, DateTimeOffset? until, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> UnblockAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>Soft delete: the row stays for referential integrity, the account stops working.</summary>
        Task<AdminOperationResult> SoftDeleteAsync(string id, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> RestoreAsync(string id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken = default);

        Task<AdminOperationResult> SetRolesAsync(string id, IEnumerable<string> roles, CancellationToken cancellationToken = default);
    }

    public interface IAdminOrderService
    {
        Task<PagedResult<AdminOrderListItem>> GetOrdersAsync(OrderFilter filter, CancellationToken cancellationToken = default);

        Task<AdminOrderDetails?> GetOrderAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminOrderListItem>> GetOrdersForExportAsync(OrderFilter filter, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> ChangeStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface IAdminCategoryService
    {
        /// <summary>Categories ordered as a tree: each root immediately followed by its children.</summary>
        Task<IReadOnlyList<AdminCategoryListItem>> GetTreeAsync(CategoryFilter filter, CancellationToken cancellationToken = default);

        Task<AdminCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Root categories that may serve as a parent, excluding <paramref name="excludeId"/> and its children.</summary>
        Task<IReadOnlyList<AdminCategoryListItem>> GetParentOptionsAsync(int? excludeId = null, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> CreateAsync(AdminCategoryEditModel model, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> UpdateAsync(AdminCategoryEditModel model, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    /// <summary>Complaints and reviews: the two things an admin actually moderates.</summary>
    public interface IAdminModerationService
    {
        Task<PagedResult<AdminComplaintListItem>> GetComplaintsAsync(ComplaintFilter filter, CancellationToken cancellationToken = default);

        Task<AdminComplaintListItem?> GetComplaintAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminComplaintListItem>> GetComplaintsForExportAsync(ComplaintFilter filter, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> SetComplaintStatusAsync(int id, ComplaintStatus status, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminReviewListItem>> GetReviewsAsync(ReviewFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminReviewListItem>> GetReviewsForExportAsync(ReviewFilter filter, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> DeleteReviewAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Counters for the moderation queue badges.</summary>
        Task<ModerationQueue> GetQueueAsync(CancellationToken cancellationToken = default);
    }

    public interface IAdminPaymentService
    {
        Task<PagedResult<AdminPaymentListItem>> GetPaymentsAsync(PaymentFilter filter, CancellationToken cancellationToken = default);

        Task<AdminPaymentListItem?> GetPaymentAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminPaymentListItem>> GetPaymentsForExportAsync(PaymentFilter filter, CancellationToken cancellationToken = default);

        Task<PaymentTotals> GetTotalsAsync(PaymentFilter filter, CancellationToken cancellationToken = default);

        Task<AdminOperationResult> MarkRefundedAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface IAuditLogService
    {
        Task WriteAsync(
            AuditAction action,
            string entityName,
            string? entityId,
            string? description,
            string? userId,
            string userName,
            string? ipAddress,
            AuditSeverity severity = AuditSeverity.Information,
            CancellationToken cancellationToken = default);

        Task<PagedResult<AdminAuditLogListItem>> GetLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminAuditLogListItem>> GetLogsForExportAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetEntityNamesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>Read access to order dialogs plus the ability to post as support.</summary>
    public interface IAdminChatService
    {
        Task<IReadOnlyList<AdminDialogListItem>> GetDialogsAsync(string? search, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AdminChatMessage>> GetMessagesAsync(int orderId, CancellationToken cancellationToken = default);

        Task<AdminChatMessage> SendAsync(int orderId, string senderId, string text, CancellationToken cancellationToken = default);

        Task MarkReadAsync(int orderId, string readerId, CancellationToken cancellationToken = default);
    }

    /// <summary>Builds .xlsx workbooks without a third-party library, so the panel has no extra dependency.</summary>
    public interface IExcelExportService
    {
        byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows);
    }

    public class ModerationQueue
    {
        public int PendingComplaints { get; set; }
        public int LowRatedReviews { get; set; }
        public int BlockedUsers { get; set; }
        public int StaleOrders { get; set; }
        public int UnpaidCompletedOrders { get; set; }

        public int Total => PendingComplaints + LowRatedReviews + StaleOrders + UnpaidCompletedOrders;
    }

    public class PaymentTotals
    {
        public decimal Completed { get; set; }
        public decimal Pending { get; set; }
        public decimal Failed { get; set; }
        public decimal Refunded { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Result of a state-changing admin action. Controllers turn this straight into a flash message,
    /// which keeps "did it work and what do I tell the user" in one place.
    /// </summary>
    public class AdminOperationResult
    {
        public bool Succeeded { get; init; }

        public string Message { get; init; } = string.Empty;

        public static AdminOperationResult Ok(string message) =>
            new() { Succeeded = true, Message = message };

        public static AdminOperationResult Fail(string message) =>
            new() { Succeeded = false, Message = message };
    }
}
