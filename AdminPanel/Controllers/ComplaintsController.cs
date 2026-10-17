using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    [Authorize(Policy = AppRoles.ModerationPolicy)]
    public class ComplaintsController : AdminControllerBase
    {
        private readonly IAdminModerationService _moderation;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public ComplaintsController(
            IAdminModerationService moderation,
            IExcelExportService excel,
            IAuditLogService audit)
        {
            _moderation = moderation;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index([FromQuery] ComplaintFilter filter, CancellationToken cancellationToken)
        {
            var model = new ListViewModel<AdminComplaintListItem, ComplaintFilter>
            {
                Filter = filter,
                Result = await _moderation.GetComplaintsAsync(filter, cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var complaint = await _moderation.GetComplaintAsync(id, cancellationToken);
            if (complaint == null)
            {
                FlashError($"Complaint #{id} not found.");
                return RedirectToAction(nameof(Index));
            }

            return View(complaint);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, ComplaintStatus status, string? returnUrl, CancellationToken cancellationToken)
        {
            var result = await _moderation.SetComplaintStatusAsync(id, status, cancellationToken);
            Flash(result);
            await AuditAsync(
                _audit,
                status == ComplaintStatus.Resolved ? AuditAction.Approve : AuditAction.Reject,
                "Complaint",
                id.ToString(),
                result);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Export([FromQuery] ComplaintFilter filter, CancellationToken cancellationToken)
        {
            var complaints = await _moderation.GetComplaintsForExportAsync(filter, cancellationToken);

            var headers = new[]
            {
                "Id", "Created", "Status", "Reason", "Description",
                "From", "Against", "Complaints against this user"
            };

            var rows = complaints.Select(c => (IReadOnlyList<object?>)new object?[]
            {
                c.Id,
                c.CreatedAt,
                c.Status.ToString(),
                c.Reason,
                c.Description,
                c.SenderName,
                c.TargetUserName ?? "—",
                c.TargetComplaintCount
            });

            var file = _excel.Build("Complaints", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "Complaint", null,
                $"Exported {complaints.Count} complaint(s) to Excel");

            return Xlsx(file, "servicehub-complaints");
        }
    }
}
