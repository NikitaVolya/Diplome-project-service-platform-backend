using AdminPanel.Extensions;
using AdminPanel.Hubs;
using DAL.Context;
using DAL.Seed;
using Microsoft.EntityFrameworkCore;

namespace AdminPanel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.Configure<SeedOptions>(
                builder.Configuration.GetSection(SeedOptions.SectionName));

            builder.Services.AddAdminIdentity();
            builder.Services.AddAdminAuthorization();
            builder.Services.AddAdminServices();

            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();

            // Renders "27.08.2026" style dates and "1 234,56" money consistently for every admin,
            // regardless of the browser locale they happen to be using.
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Dashboard}/{action=Index}/{id?}");

            app.MapHub<AdminChatHub>("/hubs/admin-chat");

            await PrepareDatabaseAsync(app);

            await app.RunAsync();
        }

        /// <summary>
        /// Applies pending migrations and runs the seeder. Both steps are optional and controlled from
        /// configuration so that a production deployment can migrate through its own pipeline instead.
        /// </summary>
        private static async Task PrepareDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                if (app.Configuration.GetValue("Database:AutoMigrate", true))
                {
                    var db = services.GetRequiredService<ApplicationDbContext>();
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database is up to date");
                }

                var seeder = services.GetRequiredService<IDatabaseSeeder>();
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                // A database that is not reachable yet must not crash the host: the panel starts and
                // shows the error on the first request instead of failing silently at boot.
                logger.LogError(ex, "Database preparation failed");
            }
        }
    }
}
