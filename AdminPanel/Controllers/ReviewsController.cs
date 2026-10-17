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
    public class ReviewsController : AdminControllerBase
    {
        private readonly IAdminModerationService _moderation;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public ReviewsController(
            IAdminModerationService moderation,
            IExcelExportService excel,
            IAuditLogService audit)
        {
            _moderation = moderation;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index([FromQuery] ReviewFilter filter, CancellationToken cancellationToken)
        {
            var model = new ListViewModel<AdminReviewListItem, ReviewFilter>
            {
                Filter = filter,
                Result = await _moderation.GetReviewsAsync(filter, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl, CancellationToken cancellationToken)
        {
            var result = await _moderation.DeleteReviewAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Delete, "Review", id.ToString(), result, AuditSeverity.Warning);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Export([FromQuery] ReviewFilter filter, CancellationToken cancellationToken)
        {
            var reviews = await _moderation.GetReviewsForExportAsync(filter, cancellationToken);

            var headers = new[] { "Id", "Created", "Rating", "Order", "Author", "About", "Comment" };

            var rows = reviews.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.Id,
                r.CreatedAt,
                r.Rating,
                r.OrderTitle,
                r.AuthorName,
                r.TargetUserName,
                r.Comment
            });

            var file = _excel.Build("Reviews", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "Review", null,
                $"Exported {reviews.Count} review(s) to Excel");

            return Xlsx(file, "servicehub-reviews");
        }
    }
}
