using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    public class OrdersController : AdminControllerBase
    {
        private readonly IAdminOrderService _orders;
        private readonly IAdminCategoryService _categories;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public OrdersController(
            IAdminOrderService orders,
            IAdminCategoryService categories,
            IExcelExportService excel,
            IAuditLogService audit)
        {
            _orders = orders;
            _categories = categories;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index([FromQuery] OrderFilter filter, CancellationToken cancellationToken)
        {
            var model = new OrdersViewModel
            {
                Filter = filter,
                Result = await _orders.GetOrdersAsync(filter, cancellationToken),
                Categories = await _categories.GetTreeAsync(new CategoryFilter(), cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var order = await _orders.GetOrderAsync(id, cancellationToken);
            if (order == null)
            {
                FlashError($"Order #{id} not found.");
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        [HttpPost]
        [Authorize(Policy = AppRoles.ModerationPolicy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, OrderStatus status, string? returnUrl, CancellationToken cancellationToken)
        {
            var result = await _orders.ChangeStatusAsync(id, status, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Update, "Order", id.ToString(), result);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _orders.DeleteAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Delete, "Order", id.ToString(), result, AuditSeverity.Critical);

            return result.Succeeded
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Export([FromQuery] OrderFilter filter, CancellationToken cancellationToken)
        {
            var orders = await _orders.GetOrdersForExportAsync(filter, cancellationToken);

            var headers = new[]
            {
                "Id", "Title", "Category", "Status", "Price", "Customer", "Executor",
                "Address", "Applications", "Created", "Execution date"
            };

            var rows = orders.Select(o => (IReadOnlyList<object?>)new object?[]
            {
                o.Id,
                o.Title,
                o.CategoryName,
                o.Status.ToString(),
                o.Price,
                o.CustomerName,
                o.ExecutorName ?? "—",
                o.Address,
                o.ApplicationsCount,
                o.CreatedAt,
                o.ExecutionAt
            });

            var file = _excel.Build("Orders", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "Order", null,
                $"Exported {orders.Count} order(s) to Excel");

            return Xlsx(file, "servicehub-orders");
        }
    }
}
