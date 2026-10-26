using AdminPanel.Extensions;
using AdminPanel.Hubs;
using AdminPanel.Localization;
using AdminPanel.Services;
using BLL.Options;
using DAL.Context;
using DAL.Seed;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AdminPanel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var startupLogs = new ConcurrentQueue<string>();

            builder.Logging.SetMinimumLevel(LogLevel.Information);

            builder.Logging.AddFilter(
                "Microsoft.EntityFrameworkCore",
                LogLevel.Information
            );

            builder.Logging.AddProvider(
                new InMemoryLoggerProvider(startupLogs)
            );

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 4, 11)),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null
                        );
                    }
                );
            });

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

            // Перед тим як приймати запити: створюємо базу, якщо її немає, накочуємо нові міграції
            // й заповнюємо довідники. Деталі й налаштування — у Services/DatabaseStartup.cs.
            await DatabaseStartup.PrepareAsync(app);

            app.MapGet("/diagnostics/startup", () =>
            {
                if (!app.Environment.IsDevelopment())
                {
                    return Results.NotFound();
                }

                return Results.Text(
                    string.Join(Environment.NewLine, startupLogs),
                    "text/plain; charset=utf-8"
                );
            }).AllowAnonymous();

            await app.RunAsync();
        }
    }
}
