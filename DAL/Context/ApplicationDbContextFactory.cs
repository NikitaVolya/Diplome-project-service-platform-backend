using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAL.Context
{
    /// <summary>
    /// Фабрика для етапу розробки, щоб «dotnet ef migrations add ...» можна було виконувати з папки DAL,
    /// не піднімаючи ані API, ані хост адмінпанелі.
    /// Рядок підключення можна перевизначити змінною середовища SERVICEHUB_CONNECTION.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        private const string DefaultConnection =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=KabanchikDB;Integrated Security=True;" +
            "Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;" +
            "Application Intent=ReadWrite;Multi Subnet Failover=False";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("SERVICEHUB_CONNECTION") ?? DefaultConnection;

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
