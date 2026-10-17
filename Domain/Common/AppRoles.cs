namespace Domain.Common
{
    /// <summary>
    /// Назви ролей, які використовуються по всій платформі. Винесені в константи, щоб атрибути
    /// [Authorize] в адмінці та код заповнення бази ніколи не розійшлися через одну описку.
    /// </summary>
    public static class AppRoles
    {
        /// <summary>Повний доступ, зокрема керування ролями та незворотні дії.</summary>
        public const string Admin = "Admin";

        /// <summary>Працює зі скаргами, відгуками та користувачами, але не з ролями й платежами.</summary>
        public const string Moderator = "Moderator";

        /// <summary>Доступ лише для перегляду плюс чат підтримки.</summary>
        public const string Support = "Support";

        /// <summary>Звичайний користувач платформи, який створює замовлення.</summary>
        public const string Customer = "Customer";

        /// <summary>Звичайний користувач платформи, який виконує замовлення.</summary>
        public const string Executor = "Executor";

        /// <summary>Усі ролі, які створює код початкового заповнення бази.</summary>
        public static readonly string[] All = { Admin, Moderator, Support, Customer, Executor };

        /// <summary>Ролі, яким узагалі дозволено входити у вебпанель адміністрування.</summary>
        public static readonly string[] StaffRoles = { Admin, Moderator, Support };

        public const string StaffPolicy = "StaffOnly";
        public const string AdminOnlyPolicy = "AdminOnly";
        public const string ModerationPolicy = "Moderation";
    }
}
