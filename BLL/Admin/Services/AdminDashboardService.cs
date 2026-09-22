using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    // Агрегація по робочих таблицях лише для читання. Усе рахується на вимогу, а не береться
    // з таблиці Statistics, тому дашборд показує правильні числа навіть тоді, коли нічне
    // завдання підрахунку статистики ще не відпрацювало.
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ApplicationDbContext _db;

        public AdminDashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var previousMonthStart = monthStart.AddMonths(-1);

            var summary = new DashboardSummary
            {
                TotalUsers = await _db.Users.CountAsync(u => !u.IsDeleted, cancellationToken),
                NewUsersToday = await _db.Users.CountAsync(u => u.CreatedAt >= today, cancellationToken),
                NewUsersThisMonth = await _db.Users.CountAsync(u => u.CreatedAt >= monthStart, cancellationToken),
                BlockedUsers = await _db.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > now, cancellationToken),

                TotalOrders = await _db.Orders.CountAsync(cancellationToken),
                OrdersToday = await _db.Orders.CountAsync(o => o.CreatedAt >= today, cancellationToken),
                OpenOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken),
                InProgressOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.InProgress, cancellationToken),
                CompletedOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Completed, cancellationToken),
                CancelledOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled, cancellationToken),

                PendingComplaints = await _db.Complaints.CountAsync(c => c.Status == ComplaintStatus.Pending, cancellationToken),
                TotalComplaints = await _db.Complaints.CountAsync(cancellationToken),
                TotalReviews = await _db.Reviews.CountAsync(cancellationToken),
                LowRatedReviews = await _db.Reviews.CountAsync(r => r.Rating <= 2, cancellationToken),

                ActiveCategories = await _db.Categories.CountAsync(c => c.IsActive, cancellationToken)
            };

            var completedPayments = _db.Payments.Where(p => p.Status == PaymentStatus.Completed);

            summary.TotalRevenue = await SumOrZeroAsync(completedPayments, cancellationToken);
            summary.RevenueThisMonth = await SumOrZeroAsync(
                completedPayments.Where(p => p.PaidAt != null && p.PaidAt >= monthStart), cancellationToken);

            var previousMonthRevenue = await SumOrZeroAsync(
                completedPayments.Where(p => p.PaidAt != null && p.PaidAt >= previousMonthStart && p.PaidAt < monthStart),
                cancellationToken);

            var ordersThisMonth = await _db.Orders.CountAsync(o => o.CreatedAt >= monthStart, cancellationToken);
            var ordersPreviousMonth = await _db.Orders.CountAsync(
                o => o.CreatedAt >= previousMonthStart && o.CreatedAt < monthStart, cancellationToken);

            summary.OrdersMonthOverMonth = PercentageChange(ordersPreviousMonth, ordersThisMonth);
            summary.RevenueMonthOverMonth = PercentageChange((double)previousMonthRevenue, (double)summary.RevenueThisMonth);

            summary.AverageOrderValue = summary.CompletedOrders == 0
                ? 0
                : Math.Round(summary.TotalRevenue / summary.CompletedOrders, 2);

            summary.AverageRating = summary.TotalReviews == 0
                ? 0
                : Math.Round(await _db.Reviews.AverageAsync(r => (double)r.Rating, cancellationToken), 2);

            summary.RecentOrders = await _db.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(8)
                .Select(o => new AdminOrderListItem
                {
                    Id = o.Id,
                    Title = o.Title,
                    Price = o.Price,
                    Status = o.Status,
                    CategoryId = o.CategoryId,
                    CategoryName = o.Category.Name,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    ExecutorId = o.ExecutorId,
                    ExecutorName = o.Executor == null ? null : o.Executor.FirstName + " " + o.Executor.LastName,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(cancellationToken);

            summary.RecentComplaints = await _db.Complaints
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(6)
                .Select(c => new AdminComplaintListItem
                {
                    Id = c.Id,
                    SenderId = c.SenderId,
                    SenderName = c.Sender.FirstName + " " + c.Sender.LastName,
                    TargetUserId = c.TargetUserId,
                    TargetUserName = c.TargetUser == null ? null : c.TargetUser.FirstName + " " + c.TargetUser.LastName,
                    Reason = c.Reason,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            summary.TopCategories = await _db.Orders
                .AsNoTracking()
                .GroupBy(o => new { o.CategoryId, o.Category.Name })
                .Select(g => new TopCategoryItem
                {
                    CategoryId = g.Key.CategoryId,
                    Name = g.Key.Name,
                    OrdersCount = g.Count(),
                    Revenue = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.Price) ?? 0m
                })
                .OrderByDescending(c => c.OrdersCount)
                .Take(6)
                .ToListAsync(cancellationToken);

            summary.TopExecutors = await _db.Orders
                .AsNoTracking()
                .Where(o => o.ExecutorId != null && o.Status == OrderStatus.Completed)
                .GroupBy(o => new { o.ExecutorId, o.Executor!.FirstName, o.Executor.LastName, o.Executor.AvatarUrl })
                .Select(g => new TopExecutorItem
                {
                    UserId = g.Key.ExecutorId!,
                    Name = g.Key.FirstName + " " + g.Key.LastName,
                    AvatarUrl = g.Key.AvatarUrl,
                    CompletedOrders = g.Count(),
                    Earned = g.Sum(o => (decimal?)o.Price) ?? 0m
                })
                .OrderByDescending(e => e.CompletedOrders)
                .Take(6)
                .ToListAsync(cancellationToken);

            if (summary.TopExecutors.Count > 0)
            {
                var executorIds = summary.TopExecutors.Select(e => e.UserId).ToList();

                var ratings = await _db.Reviews
                    .AsNoTracking()
                    .Where(r => executorIds.Contains(r.TargetUserId))
                    .GroupBy(r => r.TargetUserId)
                    .Select(g => new { UserId = g.Key, Average = g.Average(r => (double)r.Rating) })
                    .ToListAsync(cancellationToken);

                foreach (var executor in summary.TopExecutors)
                {
                    var rating = ratings.FirstOrDefault(r => r.UserId == executor.UserId);
                    executor.AverageRating = rating == null ? 0 : Math.Round(rating.Average, 2);
                }
            }

            return summary;
        }

        public async Task<ChartData> GetOrdersChartAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            days = Math.Clamp(days, 7, 180);
            var from = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var raw = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= from)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Created = g.Count(),
                    Completed = g.Count(o => o.Status == OrderStatus.Completed)
                })
                .ToListAsync(cancellationToken);

            var byDay = raw.ToDictionary(x => x.Day);

            var chart = new ChartData();
            var created = new ChartSeries { Label = "Created" };
            var completed = new ChartSeries { Label = "Completed" };

            for (var i = 0; i < days; i++)
            {
                var day = from.AddDays(i);
                chart.Labels.Add(day.ToString("dd MMM"));
                byDay.TryGetValue(day, out var row);
                created.Data.Add(row?.Created ?? 0);
                completed.Data.Add(row?.Completed ?? 0);
            }

            chart.Series.Add(created);
            chart.Series.Add(completed);
            return chart;
        }

        public async Task<ChartData> GetRevenueChartAsync(int months = 12, CancellationToken cancellationToken = default)
        {
            months = Math.Clamp(months, 3, 36);
            var today = DateTime.UtcNow.Date;
            var from = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));

            var raw = await _db.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt != null && p.PaidAt >= from)
                .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt!.Value.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var chart = new ChartData();
            var revenue = new ChartSeries { Label = "Revenue, UAH" };
            var payments = new ChartSeries { Label = "Payments" };

            for (var i = 0; i < months; i++)
            {
                var month = from.AddMonths(i);
                chart.Labels.Add(month.ToString("MMM yyyy"));

                var row = raw.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month);
                revenue.Data.Add(row?.Amount ?? 0m);
                payments.Data.Add(row?.Count ?? 0);
            }

            chart.Series.Add(revenue);
            chart.Series.Add(payments);
            return chart;
        }

        public async Task<ChartData> GetUsersChartAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            days = Math.Clamp(days, 7, 180);
            var from = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var raw = await _db.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= from)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var byDay = raw.ToDictionary(x => x.Day, x => x.Count);

            var chart = new ChartData();
            var series = new ChartSeries { Label = "New users" };

            for (var i = 0; i < days; i++)
            {
                var day = from.AddDays(i);
                chart.Labels.Add(day.ToString("dd MMM"));
                series.Data.Add(byDay.TryGetValue(day, out var count) ? count : 0);
            }

            chart.Series.Add(series);
            return chart;
        }

        public async Task<ChartData> GetOrderStatusChartAsync(CancellationToken cancellationToken = default)
        {
            var raw = await _db.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var chart = new ChartData();
            var series = new ChartSeries { Label = "Orders" };

            foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
            {
                chart.Labels.Add(FriendlyStatus(status));
                series.Data.Add(raw.FirstOrDefault(r => r.Status == status)?.Count ?? 0);
            }

            chart.Series.Add(series);
            return chart;
        }

        public async Task<ChartData> GetCategoryChartAsync(int top = 8, CancellationToken cancellationToken = default)
        {
            top = Math.Clamp(top, 3, 20);

            var raw = await _db.Orders
                .AsNoTracking()
                .GroupBy(o => o.Category.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToListAsync(cancellationToken);

            var chart = new ChartData();
            var series = new ChartSeries { Label = "Orders" };

            foreach (var row in raw)
            {
                chart.Labels.Add(row.Name);
                series.Data.Add(row.Count);
            }

            chart.Series.Add(series);
            return chart;
        }

        public async Task<ChartData> GetRatingChartAsync(CancellationToken cancellationToken = default)
        {
            var raw = await _db.Reviews
                .AsNoTracking()
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var chart = new ChartData();
            var series = new ChartSeries { Label = "Reviews" };

            for (var rating = 1; rating <= 5; rating++)
            {
                chart.Labels.Add($"{rating} ★");
                series.Data.Add(raw.FirstOrDefault(r => r.Rating == rating)?.Count ?? 0);
            }

            chart.Series.Add(series);
            return chart;
        }

        public async Task<IReadOnlyList<Statistic>> GetDailyBreakdownAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var fromDate = from.Date;
            var toExclusive = to.Date.AddDays(1);

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < toExclusive)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(o => o.Status == OrderStatus.Completed)
                })
                .ToListAsync(cancellationToken);

            var revenue = await _db.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed
                            && p.PaidAt != null
                            && p.PaidAt >= fromDate
                            && p.PaidAt < toExclusive)
                .GroupBy(p => p.PaidAt!.Value.Date)
                .Select(g => new { Day = g.Key, Amount = g.Sum(p => p.Amount) })
                .ToListAsync(cancellationToken);

            var users = await _db.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= fromDate && u.CreatedAt < toExclusive)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var result = new List<Statistic>();

            for (var day = fromDate; day < toExclusive; day = day.AddDays(1))
            {
                var orderRow = orders.FirstOrDefault(o => o.Day == day);
                var revenueRow = revenue.FirstOrDefault(r => r.Day == day);
                var userRow = users.FirstOrDefault(u => u.Day == day);

                result.Add(new Statistic
                {
                    Date = day,
                    TotalOrders = orderRow?.Total ?? 0,
                    CompletedOrders = orderRow?.Completed ?? 0,
                    TotalRevenue = revenueRow?.Amount ?? 0m,
                    NewUsersCount = userRow?.Count ?? 0
                });
            }

            return result.OrderByDescending(s => s.Date).ToList();
        }

        /// <summary>Підписи графіків читають люди, тому «InProgress» перетворюється на «In progress».</summary>
        private static string FriendlyStatus(OrderStatus status) => status switch
        {
            OrderStatus.InProgress => "In progress",
            _ => status.ToString()
        };

        private static async Task<decimal> SumOrZeroAsync(IQueryable<Payment> query, CancellationToken cancellationToken)
        {
            // Сума по порожній вибірці в SQL дорівнює null, тому спочатку проєктуємо в decimal?.
            return await query.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        }

        private static double PercentageChange(double previous, double current)
        {
            if (Math.Abs(previous) < 0.0001)
            {
                return current > 0 ? 100 : 0;
            }

            return Math.Round((current - previous) / previous * 100, 1);
        }
    }
}
