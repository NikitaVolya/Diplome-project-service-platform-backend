namespace DAL.Seed
{
    /// <summary>
    /// Bound from the "Seed" section of appsettings.json.
    /// </summary>
    public class SeedOptions
    {
        public const string SectionName = "Seed";

        /// <summary>Create roles and the built-in administrator account on start-up.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Fill an empty database with demo users, orders, payments and reviews so the dashboard and
        /// charts have something to show. Ignored when the database already contains orders.
        /// </summary>
        public bool DemoData { get; set; }

        public string AdminEmail { get; set; } = "admin@servicehub.local";

        public string AdminPassword { get; set; } = "Admin#2026";

        public string AdminFirstName { get; set; } = "Platform";

        public string AdminLastName { get; set; } = "Administrator";

        public int DemoUsers { get; set; } = 40;

        public int DemoOrders { get; set; } = 160;

        /// <summary>How far back demo data is spread; also the span the dashboard charts cover.</summary>
        public int DemoHistoryDays { get; set; } = 120;
    }
}
