using AdminPanel.Models;
using BLL.Admin.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    public class StatisticsController : AdminControllerBase
    {
        private readonly IAdminDashboardService _dashboard;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public StatisticsController(
            IAdminDashboardService dashboard,
            IExcelExportService excel,
            IAuditLogService audit)
        {
            _dashboard = dashboard;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, CancellationToken cancellationToken)
        {
            var (start, end) = Normalise(from, to);
            var days = (int)(end - start).TotalDays + 1;

            var model = new StatisticsViewModel
            {
                From = start,
                To = end,
                Summary = await _dashboard.GetSummaryAsync(cancellationToken),
                OrdersChart = await _dashboard.GetOrdersChartAsync(days, cancellationToken),
                RevenueChart = await _dashboard.GetRevenueChartAsync(12, cancellationToken),
                UsersChart = await _dashboard.GetUsersChartAsync(days, cancellationToken),
                RatingChart = await _dashboard.GetRatingChartAsync(cancellationToken),
                CategoryChart = await _dashboard.GetCategoryChartAsync(10, cancellationToken),
                Daily = await _dashboard.GetDailyBreakdownAsync(start, end, cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> Export(DateTime? from, DateTime? to, CancellationToken cancellationToken)
        {
            var (start, end) = Normalise(from, to);
            var daily = await _dashboard.GetDailyBreakdownAsync(start, end, cancellationToken);

            var headers = new[] { "Date", "Orders", "Completed", "Completion rate, %", "Revenue", "New users" };

            var rows = daily.Select(d => (IReadOnlyList<object?>)new object?[]
            {
                d.Date,
                d.TotalOrders,
                d.CompletedOrders,
                d.TotalOrders == 0 ? 0m : Math.Round(d.CompletedOrders * 100m / d.TotalOrders, 1),
                d.TotalRevenue,
                d.NewUsersCount
            });

            var file = _excel.Build("Statistics", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "Statistic", null,
                $"Exported statistics for {start:yyyy-MM-dd}…{end:yyyy-MM-dd}");

            return Xlsx(file, "servicehub-statistics");
        }

        // За замовчуванням — останні 30 днів; діапазон ніколи не буває перевернутим.
        private static (DateTime From, DateTime To) Normalise(DateTime? from, DateTime? to)
        {
            var end = (to ?? DateTime.UtcNow).Date;
            var start = (from ?? end.AddDays(-29)).Date;

            if (start > end)
            {
                (start, end) = (end, start);
            }

            // Рік щоденних рядків — це вже довга таблиця; усе більше варто дивитися у вивантаженні, а не на екрані.
            if ((end - start).TotalDays > 366)
            {
                start = end.AddDays(-366);
            }

            return (start, end);
        }
    }
}
