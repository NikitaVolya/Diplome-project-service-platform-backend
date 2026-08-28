namespace DAL.Seed
{
    /// <summary>
    /// Прив'язується до секції «Seed» у appsettings.json.
    /// </summary>
    public class SeedOptions
    {
        public const string SectionName = "Seed";

        /// <summary>Створювати ролі та вбудований акаунт адміністратора під час запуску.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Заповнити порожню базу демонстраційними користувачами, замовленнями, платежами та відгуками,
        /// щоб дашборду й графікам було що показати. Ігнорується, якщо в базі вже є замовлення.
        /// </summary>
        public bool DemoData { get; set; }

        public string AdminEmail { get; set; } = "admin@servicehub.local";

        public string AdminPassword { get; set; } = "Admin#2026";

        public string AdminFirstName { get; set; } = "Platform";

        public string AdminLastName { get; set; } = "Administrator";

        public int DemoUsers { get; set; } = 40;

        public int DemoOrders { get; set; } = 160;

        /// <summary>На скільки днів назад розкидані демо-дані; це ж і період, який охоплюють графіки дашборда.</summary>
        public int DemoHistoryDays { get; set; } = 120;
    }
}
