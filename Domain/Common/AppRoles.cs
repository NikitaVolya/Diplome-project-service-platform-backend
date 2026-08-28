namespace Domain.Common
{
    /// <summary>
    /// Role names used across the platform. Kept as constants so that the admin panel's
    /// [Authorize] attributes and the seeder can never drift apart through a typo.
    /// </summary>
    public static class AppRoles
    {
        /// <summary>Full access, including role management and destructive actions.</summary>
        public const string Admin = "Admin";

        /// <summary>Handles complaints, reviews and users, but not roles or payments.</summary>
        public const string Moderator = "Moderator";

        /// <summary>Read-only access plus the support chat.</summary>
        public const string Support = "Support";

        /// <summary>Ordinary platform user who places orders.</summary>
        public const string Customer = "Customer";

        /// <summary>Ordinary platform user who performs orders.</summary>
        public const string Executor = "Executor";

        /// <summary>Every role the seeder creates.</summary>
        public static readonly string[] All = { Admin, Moderator, Support, Customer, Executor };

        /// <summary>Roles that are allowed to sign in to the web admin panel at all.</summary>
        public static readonly string[] StaffRoles = { Admin, Moderator, Support };

        public const string StaffPolicy = "StaffOnly";
        public const string AdminOnlyPolicy = "AdminOnly";
        public const string ModerationPolicy = "Moderation";
    }
}
