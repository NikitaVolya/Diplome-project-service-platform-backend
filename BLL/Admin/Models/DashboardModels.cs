namespace BLL.Admin.Models
{
    /// <summary>
    /// Everything the dashboard shows above the charts. One instance is built per page load.
    /// </summary>
    public class DashboardSummary
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int BlockedUsers { get; set; }

        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public int OpenOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AverageOrderValue { get; set; }

        public int PendingComplaints { get; set; }
        public int TotalComplaints { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int LowRatedReviews { get; set; }

        public int ActiveCategories { get; set; }

        /// <summary>Percentage change of this month's order count against the previous month.</summary>
        public double OrdersMonthOverMonth { get; set; }

        /// <summary>Percentage change of this month's revenue against the previous month.</summary>
        public double RevenueMonthOverMonth { get; set; }

        public double CompletionRate =>
            TotalOrders == 0 ? 0 : Math.Round(CompletedOrders * 100.0 / TotalOrders, 1);

        public IReadOnlyList<AdminOrderListItem> RecentOrders { get; set; } = Array.Empty<AdminOrderListItem>();
        public IReadOnlyList<AdminComplaintListItem> RecentComplaints { get; set; } = Array.Empty<AdminComplaintListItem>();
        public IReadOnlyList<TopCategoryItem> TopCategories { get; set; } = Array.Empty<TopCategoryItem>();
        public IReadOnlyList<TopExecutorItem> TopExecutors { get; set; } = Array.Empty<TopExecutorItem>();
    }

    public class TopCategoryItem
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopExecutorItem
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int CompletedOrders { get; set; }
        public decimal Earned { get; set; }
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// Provider-agnostic chart payload. Serialised straight to JSON and fed to Chart.js,
    /// which keeps chart shaping in one place instead of spread across views.
    /// </summary>
    public class ChartData
    {
        public List<string> Labels { get; set; } = new();

        public List<ChartSeries> Series { get; set; } = new();

        public static ChartData Empty() => new();
    }

    public class ChartSeries
    {
        public string Label { get; set; } = string.Empty;

        public List<decimal> Data { get; set; } = new();

        /// <summary>Optional explicit colour; when null the view falls back to its palette.</summary>
        public string? Color { get; set; }
    }
}
