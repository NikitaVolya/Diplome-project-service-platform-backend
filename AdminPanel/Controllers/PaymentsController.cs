using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    /// Фінансові дані доступні лише адміністраторам: модератори працюють із контентом, а не з грошима.
    [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
    public class PaymentsController : AdminControllerBase
    {
        private readonly IAdminPaymentService _payments;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public PaymentsController(
            IAdminPaymentService payments,
            IExcelExportService excel,
            IAuditLogService audit)
        {
            _payments = payments;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index([FromQuery] PaymentFilter filter, CancellationToken cancellationToken)
        {
            var model = new PaymentsViewModel
            {
                Filter = filter,
                Result = await _payments.GetPaymentsAsync(filter, cancellationToken),
                Totals = await _payments.GetTotalsAsync(filter, cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var payment = await _payments.GetPaymentAsync(id, cancellationToken);
            if (payment == null)
            {
                FlashError($"Payment #{id} not found.");
                return RedirectToAction(nameof(Index));
            }

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id, string? returnUrl, CancellationToken cancellationToken)
        {
            var result = await _payments.MarkRefundedAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Update, "Payment", id.ToString(), result, AuditSeverity.Critical);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Export([FromQuery] PaymentFilter filter, CancellationToken cancellationToken)
        {
            var payments = await _payments.GetPaymentsForExportAsync(filter, cancellationToken);

            var headers = new[]
            {
                "Id", "Created", "Paid at", "Status", "Provider", "Amount", "Currency",
                "Order", "Payer", "Transaction id"
            };

            var rows = payments.Select(p => (IReadOnlyList<object?>)new object?[]
            {
                p.Id,
                p.CreatedAt,
                p.PaidAt,
                p.Status.ToString(),
                p.Provider.ToString(),
                p.Amount,
                p.Currency,
                p.OrderTitle,
                p.UserName,
                p.ExternalTransactionId
            });

            var file = _excel.Build("Payments", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "Payment", null,
                $"Exported {payments.Count} payment(s) to Excel");

            return Xlsx(file, "servicehub-payments");
        }
    }
}
