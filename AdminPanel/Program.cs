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

            // HttpContextAccessor потрібен сервісам, яким треба знати поточного користувача
            // та його IP-адресу — наприклад, для запису в журнал дій.
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

        // Застосовує міграції, яких бракує, і запускає заповнення бази початковими даними.
        // Обидва кроки необов'язкові й керуються конфігурацією, щоб на проді міграції
        // можна було виконувати власним конвеєром розгортання.
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
                // Недоступна база не повинна валити застосунок: панель стартує й покаже помилку
                // на першому ж запиті, замість того щоб мовчки впасти під час запуску.
                logger.LogError(ex, "Database preparation failed");
            }
        }
    }
}
