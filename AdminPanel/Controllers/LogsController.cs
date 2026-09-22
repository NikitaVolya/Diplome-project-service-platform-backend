using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    /// Журнал дій лише для перегляду. Видалення записів свідомо не передбачено:
    /// журнал, який адміністратор може стерти, вже не є журналом аудиту.
    [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
    public class LogsController : AdminControllerBase
    {
        private readonly IAuditLogService _audit;
        private readonly IExcelExportService _excel;

        public LogsController(IAuditLogService audit, IExcelExportService excel)
        {
            _audit = audit;
            _excel = excel;
        }

        public async Task<IActionResult> Index([FromQuery] AuditLogFilter filter, CancellationToken cancellationToken)
        {
            var model = new LogsViewModel
            {
                Filter = filter,
                Result = await _audit.GetLogsAsync(filter, cancellationToken),
                EntityNames = await _audit.GetEntityNamesAsync(cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> Export([FromQuery] AuditLogFilter filter, CancellationToken cancellationToken)
        {
            var logs = await _audit.GetLogsForExportAsync(filter, cancellationToken);

            var headers = new[] { "Id", "Time (UTC)", "Severity", "Action", "Entity", "Entity id", "User", "IP", "Description" };

            var rows = logs.Select(l => (IReadOnlyList<object?>)new object?[]
            {
                l.Id,
                l.CreatedAt,
                l.Severity.ToString(),
                l.Action.ToString(),
                l.EntityName,
                l.EntityId,
                l.UserName,
                l.IpAddress,
                l.Description
            });

            var file = _excel.Build("Activity log", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "AuditLog", null,
                $"Exported {logs.Count} log entry(-ies) to Excel");

            return Xlsx(file, "servicehub-activity-log");
        }
    }
}
