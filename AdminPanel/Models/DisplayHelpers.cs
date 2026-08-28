using Domain.Models;
using Microsoft.AspNetCore.Html;

namespace AdminPanel.Models
{
    // Суто відображувальні відповідності для представлень: який колір та іконка належать якому статусу.
    // Завдяки цьому один і той самий статус виглядає однаково на всіх екранах.
    public static class Display
    {
        public static string OrderStatusClass(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "badge-soft badge-soft--warn",
            OrderStatus.InProgress => "badge-soft badge-soft--info",
            OrderStatus.Completed => "badge-soft badge-soft--ok",
            OrderStatus.Cancelled => "badge-soft badge-soft--danger",
            _ => "badge-soft badge-soft--muted"
        };

        public static string OrderStatusIcon(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "bi-hourglass-split",
            OrderStatus.InProgress => "bi-play-circle",
            OrderStatus.Completed => "bi-check-circle",
            OrderStatus.Cancelled => "bi-x-circle",
            _ => "bi-circle"
        };

        public static string OrderStatusLabel(OrderStatus status) => status switch
        {
            OrderStatus.InProgress => "In progress",
            _ => status.ToString()
        };

        public static string PaymentStatusClass(PaymentStatus status) => status switch
        {
            PaymentStatus.Completed => "badge-soft badge-soft--ok",
            PaymentStatus.Pending => "badge-soft badge-soft--warn",
            PaymentStatus.Failed => "badge-soft badge-soft--danger",
            PaymentStatus.Refunded => "badge-soft badge-soft--muted",
            _ => "badge-soft badge-soft--muted"
        };

        public static string ComplaintStatusClass(ComplaintStatus status) => status switch
        {
            ComplaintStatus.Pending => "badge-soft badge-soft--warn",
            ComplaintStatus.Resolved => "badge-soft badge-soft--ok",
            ComplaintStatus.Rejected => "badge-soft badge-soft--muted",
            _ => "badge-soft badge-soft--muted"
        };

        public static string ApplicationStatusClass(ApplicationStatus status) => status switch
        {
            ApplicationStatus.Accepted => "badge-soft badge-soft--ok",
            ApplicationStatus.Pending => "badge-soft badge-soft--warn",
            ApplicationStatus.Rejected => "badge-soft badge-soft--muted",
            _ => "badge-soft badge-soft--muted"
        };

        public static string AuditSeverityClass(AuditSeverity severity) => severity switch
        {
            AuditSeverity.Critical => "badge-soft badge-soft--danger",
            AuditSeverity.Warning => "badge-soft badge-soft--warn",
            _ => "badge-soft badge-soft--info"
        };

        public static string AuditActionIcon(AuditAction action) => action switch
        {
            AuditAction.Login => "bi-box-arrow-in-right",
            AuditAction.Logout => "bi-box-arrow-right",
            AuditAction.LoginFailed => "bi-shield-exclamation",
            AuditAction.Create => "bi-plus-circle",
            AuditAction.Update => "bi-pencil",
            AuditAction.Delete => "bi-trash",
            AuditAction.Block => "bi-slash-circle",
            AuditAction.Unblock => "bi-unlock",
            AuditAction.Approve => "bi-check2-circle",
            AuditAction.Reject => "bi-x-circle",
            AuditAction.RoleChange => "bi-person-badge",
            AuditAction.Export => "bi-file-earmark-spreadsheet",
            _ => "bi-dot"
        };

        public static string RoleClass(string role) => role switch
        {
            AppRolesNames.Admin => "badge-soft badge-soft--danger",
            AppRolesNames.Moderator => "badge-soft badge-soft--warn",
            AppRolesNames.Support => "badge-soft badge-soft--info",
            AppRolesNames.Executor => "badge-soft badge-soft--brand",
            _ => "badge-soft badge-soft--muted"
        };

        // Малює оцінку від 1 до 5 у вигляді заповнених і порожніх зірок.
        public static IHtmlContent Stars(int rating)
        {
            var filled = Math.Clamp(rating, 0, 5);
            var html = "<span class=\"stars\">" +
                       string.Concat(Enumerable.Repeat("<i class=\"bi bi-star-fill\"></i>", filled)) +
                       string.Concat(Enumerable.Repeat("<i class=\"bi bi-star is-empty\"></i>", 5 - filled)) +
                       "</span>";

            return new HtmlString(html);
        }

        // Текст на кшталт «3 хв тому» для свіжих подій; після тижня — звичайна дата.
        public static string Relative(DateTime utc)
        {
            var delta = DateTime.UtcNow - utc;

            if (delta.TotalSeconds < 60) return "just now";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} min ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} h ago";
            if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} d ago";

            return utc.ToString("dd MMM yyyy");
        }

        public static string Money(decimal amount, string currency = "UAH") =>
            $"{amount:N2} {currency}";

        public static string TrendClass(double percentage) => percentage switch
        {
            > 0.05 => "trend trend--up",
            < -0.05 => "trend trend--down",
            _ => "trend trend--flat"
        };

        public static string TrendIcon(double percentage) => percentage switch
        {
            > 0.05 => "bi-arrow-up-right",
            < -0.05 => "bi-arrow-down-right",
            _ => "bi-dash"
        };

        // Ініціали для кружечків-аватарів. 
        public static string Initials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var parts = name
                .Split(new[] { ' ', '.', '@', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => char.IsLetter(p[0]))
                .Take(2)
                .Select(p => char.ToUpperInvariant(p[0]));

            var initials = string.Concat(parts);
            return initials.Length == 0 ? "?" : initials;
        }
    }

    // Назви ролей рядковими константами: конструкція switch вимагає саме констант.
    // Дублює <see cref="Domain.Common.AppRoles"/>.
    internal static class AppRolesNames
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string Support = "Support";
        public const string Executor = "Executor";
    }
}
