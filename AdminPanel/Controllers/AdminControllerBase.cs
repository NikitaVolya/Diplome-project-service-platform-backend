using System.Security.Claims;
using System.Text.Json;
using BLL.Admin.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    /// <summary>
    /// Shared plumbing for every admin screen: who is acting, how results are reported back to the
    /// user, and how files leave the application.
    /// </summary>
    public abstract class AdminControllerBase : Controller
    {
        protected const string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        protected string CurrentUserName =>
            User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "system";

        protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        /// <summary>Shows the outcome of an action on the next page as a dismissible alert.</summary>
        protected void Flash(AdminOperationResult result)
        {
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        }

        protected void FlashSuccess(string message) => TempData["Success"] = message;

        protected void FlashError(string message) => TempData["Error"] = message;

        /// <summary>Writes an audit entry describing the outcome of an admin action.</summary>
        protected Task AuditAsync(
            IAuditLogService audit,
            AuditAction action,
            string entityName,
            string? entityId,
            AdminOperationResult result,
            AuditSeverity severity = AuditSeverity.Information)
        {
            return audit.WriteAsync(
                action,
                entityName,
                entityId,
                result.Message,
                CurrentUserId,
                CurrentUserName,
                ClientIp,
                result.Succeeded ? severity : AuditSeverity.Warning);
        }

        protected Task AuditAsync(
            IAuditLogService audit,
            AuditAction action,
            string entityName,
            string? entityId,
            string description,
            AuditSeverity severity = AuditSeverity.Information)
        {
            return audit.WriteAsync(
                action, entityName, entityId, description,
                CurrentUserId, CurrentUserName, ClientIp, severity);
        }

        /// <summary>Sends a workbook to the browser under a timestamped file name.</summary>
        protected FileContentResult Xlsx(byte[] content, string baseName)
        {
            var fileName = $"{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
            return File(content, XlsxContentType, fileName);
        }

        /// <summary>Serialises a chart payload for the inline &lt;script&gt; blocks in the views.</summary>
        protected static string ToJson(object value) =>
            JsonSerializer.Serialize(value, JsonOptions);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
