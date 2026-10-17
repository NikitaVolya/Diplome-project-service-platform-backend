using System.Security.Claims;
using System.Text.Json;
using BLL.Admin.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    public abstract class AdminControllerBase : Controller
    {
        protected const string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        protected string CurrentUserName =>
            User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "system";

        protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        protected void Flash(AdminOperationResult result)
        {
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        }

        protected void FlashSuccess(string message) => TempData["Success"] = message;

        protected void FlashError(string message) => TempData["Error"] = message;

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

        protected FileContentResult Xlsx(byte[] content, string baseName)
        {
            var fileName = $"{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
            return File(content, XlsxContentType, fileName);
        }

        protected static string ToJson(object value) =>
            JsonSerializer.Serialize(value, JsonOptions);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
