using System.ComponentModel.DataAnnotations;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Models;

namespace AdminPanel.Models
{
    /// <summary>Несе сторінку рядків разом із фільтром, який її дав, щоб представлення могло перемалювати форму.</summary>
    public class ListViewModel<TItem, TFilter> where TFilter : FilterBase
    {
        public PagedResult<TItem> Result { get; set; } = PagedResult<TItem>.Empty();

        public TFilter Filter { get; set; } = default!;
    }

    public class DashboardViewModel
    {
        public DashboardSummary Summary { get; set; } = new();
        public ChartData OrdersChart { get; set; } = ChartData.Empty();
        public ChartData RevenueChart { get; set; } = ChartData.Empty();
        public ChartData UsersChart { get; set; } = ChartData.Empty();
        public ChartData StatusChart { get; set; } = ChartData.Empty();
        public ChartData CategoryChart { get; set; } = ChartData.Empty();
        public ModerationQueue Queue { get; set; } = new();
        public int ChartDays { get; set; } = 30;
    }

    public class StatisticsViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DashboardSummary Summary { get; set; } = new();
        public ChartData OrdersChart { get; set; } = ChartData.Empty();
        public ChartData RevenueChart { get; set; } = ChartData.Empty();
        public ChartData UsersChart { get; set; } = ChartData.Empty();
        public ChartData RatingChart { get; set; } = ChartData.Empty();
        public ChartData CategoryChart { get; set; } = ChartData.Empty();
        public IReadOnlyList<Statistic> Daily { get; set; } = Array.Empty<Statistic>();
    }

    public class UsersViewModel : ListViewModel<AdminUserListItem, UserFilter>
    {
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }

    public class UserDetailsViewModel
    {
        public AdminUserDetails User { get; set; } = new();
        public IReadOnlyList<string> AllRoles { get; set; } = Array.Empty<string>();
        public bool CanManageRoles { get; set; }
    }

    public class OrdersViewModel : ListViewModel<AdminOrderListItem, OrderFilter>
    {
        public IReadOnlyList<AdminCategoryListItem> Categories { get; set; } = Array.Empty<AdminCategoryListItem>();
    }

    public class CategoriesViewModel
    {
        public IReadOnlyList<AdminCategoryListItem> Items { get; set; } = Array.Empty<AdminCategoryListItem>();
        public CategoryFilter Filter { get; set; } = new();
    }

    public class CategoryFormViewModel
    {
        public AdminCategoryEditModel Category { get; set; } = new();
        public IReadOnlyList<AdminCategoryListItem> ParentOptions { get; set; } = Array.Empty<AdminCategoryListItem>();
        public bool IsNew => Category.Id == 0;
    }

    public class ModerationViewModel
    {
        public ModerationQueue Queue { get; set; } = new();
        public IReadOnlyList<AdminComplaintListItem> PendingComplaints { get; set; } = Array.Empty<AdminComplaintListItem>();
        public IReadOnlyList<AdminReviewListItem> LowRatedReviews { get; set; } = Array.Empty<AdminReviewListItem>();
    }

    public class PaymentsViewModel : ListViewModel<AdminPaymentListItem, PaymentFilter>
    {
        public PaymentTotals Totals { get; set; } = new();
    }

    public class LogsViewModel : ListViewModel<AdminAuditLogListItem, AuditLogFilter>
    {
        public IReadOnlyList<string> EntityNames { get; set; } = Array.Empty<string>();
    }

    public class ChatViewModel
    {
        public IReadOnlyList<AdminDialogListItem> Dialogs { get; set; } = Array.Empty<AdminDialogListItem>();
        public AdminDialogListItem? Selected { get; set; }
        public IReadOnlyList<AdminChatMessage> Messages { get; set; } = Array.Empty<AdminChatMessage>();
        public string? Search { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
    }

    public class RolesViewModel
    {
        public IReadOnlyList<RoleRow> Roles { get; set; } = Array.Empty<RoleRow>();
        public IReadOnlyList<AdminUserListItem> Staff { get; set; } = Array.Empty<AdminUserListItem>();
    }

    public class RoleRow
    {
        public string Name { get; set; } = string.Empty;
        public int UsersCount { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsStaffRole { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class ErrorViewModel
    {
        public int StatusCode { get; set; } = 500;
        public string Title { get; set; } = "Something went wrong";
        public string Message { get; set; } = "An unexpected error occurred while handling your request.";
        public string? RequestId { get; set; }
    }
}
