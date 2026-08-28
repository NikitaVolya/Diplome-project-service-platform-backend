using Domain.Entities;

namespace Domain.Models
{
    /// <summary>
    /// Незмінний запис про адміністративну дію, виконану через вебпанель.
    /// Пишеться самою панеллю; через інтерфейс не редагується і не видаляється.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Ідентифікатор Identity адміністратора, який виконав дію (null для системних дій).</summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        /// <summary>Денормалізований логін виконавця — зберігається, щоб журнал пережив видалення користувача.</summary>
        public string UserName { get; set; } = string.Empty;

        public AuditAction Action { get; set; }

        /// <summary>Назва сутності, якої стосується дія, наприклад «Order», «User», «Category».</summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Первинний ключ сутності у вигляді тексту, бо в різних сутностей ключі різного типу.</summary>
        public string? EntityId { get; set; }

        public string? Description { get; set; }

        public string? IpAddress { get; set; }

        public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
    }

    public enum AuditAction
    {
        Login,
        Logout,
        LoginFailed,
        Create,
        Update,
        Delete,
        Block,
        Unblock,
        Approve,
        Reject,
        RoleChange,
        Export,
        Other
    }

    public enum AuditSeverity
    {
        Information,
        Warning,
        Critical
    }
}
