using AdminPanel.Extensions;
using AdminPanel.Hubs;
using AdminPanel.Localization;
using BLL.Options;
using DAL.Context;
using DAL.Seed;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

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

            builder.Services.Configure<EmailOptions>(
                builder.Configuration.GetSection("Email"));

            builder.Services.AddAdminIdentity();
            builder.Services.AddAdminAuthorization();
            builder.Services.AddAdminServices();

            // Переклад публічної частини: Resources/SharedResource.{мова}.resx.
            // Повідомлення валідації ([Required(ErrorMessage = ...)]) беруться з того самого файлу.
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // Переклади виводяться через @T(...), і без цього Razor кодував би кожну літеру
            // кирилиці, діакритики тощо як &#x...; — сторінка та сама, але HTML нечитабельний.
            builder.Services.Configure<WebEncoderOptions>(options =>
                options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

            builder.Services.AddControllersWithViews()
                .AddDataAnnotationsLocalization(options =>
                    options.DataAnnotationLocalizerProvider = (_, factory) =>
                        factory.Create(typeof(SharedResource)));
            builder.Services.AddSignalR();

            // Add Google authentication for users to log in with their Google accounts.
            builder.Services
                .AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
                });

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

            // Мова з перемикача внизу сторінки (cookie); без вибору — українська.
            app.UsePublicCulture();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}");

            app.MapHub<AdminChatHub>("/hubs/admin-chat");

            // Невідома адреса — сторінка 404 замість переадресації на вхід до адмінки.
            app.MapFallbackToController("Missing", "Pages");

            await PrepareDatabaseAsync(app);

            await app.RunAsync();
        }

        private static async Task PrepareDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                var db = services.GetRequiredService<ApplicationDbContext>();

                if (app.Environment.IsDevelopment()
                    && app.Configuration.GetValue("Database:RecreateOnStart", false))
                {
                    logger.LogWarning("Database:RecreateOnStart is on - dropping the database");
                    await db.Database.EnsureDeletedAsync();
                }

                if (app.Configuration.GetValue("Database:AutoMigrate", true))
                {
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database is up to date");
                }

                var seeder = services.GetRequiredService<IDatabaseSeeder>();
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database preparation failed");
            }
        }
    }
}
