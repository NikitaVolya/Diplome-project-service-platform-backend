using AdminPanel.Models;
using BLL.Admin.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    public class DashboardController : AdminControllerBase
    {
        private readonly IAdminDashboardService _dashboard;
        private readonly IAdminModerationService _moderation;

        public DashboardController(IAdminDashboardService dashboard, IAdminModerationService moderation)
        {
            _dashboard = dashboard;
            _moderation = moderation;
        }

        public async Task<IActionResult> Index(int days = 30, CancellationToken cancellationToken = default)
        {
            days = days is 7 or 14 or 30 or 90 ? days : 30;

            var model = new DashboardViewModel
            {
                ChartDays = days,
                Summary = await _dashboard.GetSummaryAsync(cancellationToken),
                OrdersChart = await _dashboard.GetOrdersChartAsync(days, cancellationToken),
                RevenueChart = await _dashboard.GetRevenueChartAsync(12, cancellationToken),
                UsersChart = await _dashboard.GetUsersChartAsync(days, cancellationToken),
                StatusChart = await _dashboard.GetOrderStatusChartAsync(cancellationToken),
                CategoryChart = await _dashboard.GetCategoryChartAsync(8, cancellationToken),
                Queue = await _moderation.GetQueueAsync(cancellationToken)
            };

            return View(model);
        }

        /// <summary>
        /// Chart data on its own, so the range selector can refresh the graphs without a full reload.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChartData(string chart, int days = 30, CancellationToken cancellationToken = default)
        {
            var data = chart?.ToLowerInvariant() switch
            {
                "orders" => await _dashboard.GetOrdersChartAsync(days, cancellationToken),
                "users" => await _dashboard.GetUsersChartAsync(days, cancellationToken),
                "revenue" => await _dashboard.GetRevenueChartAsync(days > 90 ? 24 : 12, cancellationToken),
                "status" => await _dashboard.GetOrderStatusChartAsync(cancellationToken),
                "categories" => await _dashboard.GetCategoryChartAsync(8, cancellationToken),
                "ratings" => await _dashboard.GetRatingChartAsync(cancellationToken),
                _ => null
            };

            if (data == null)
            {
                return BadRequest(new { message = $"Unknown chart \"{chart}\"." });
            }

            return Json(data);
        }
    }
}
