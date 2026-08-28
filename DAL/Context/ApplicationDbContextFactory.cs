using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAL.Context
{
    /// <summary>
    /// Design-time factory so that "dotnet ef migrations add ..." can be run from the DAL folder
    /// without spinning up the API or the admin panel host.
    /// Override the connection string with the SERVICEHUB_CONNECTION environment variable.
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
