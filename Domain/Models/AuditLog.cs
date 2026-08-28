using Domain.Entities;

namespace Domain.Models
{
    /// <summary>
    /// Immutable record of an administrative action performed through the web admin panel.
    /// Written by the panel itself; never edited or deleted through the UI.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Identity id of the administrator who performed the action (null for system actions).</summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        /// <summary>Denormalised login of the actor, kept so the log survives user deletion.</summary>
        public string UserName { get; set; } = string.Empty;

        public AuditAction Action { get; set; }

        /// <summary>Name of the affected entity, e.g. "Order", "User", "Category".</summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Primary key of the affected entity, stored as text because keys differ per entity.</summary>
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
