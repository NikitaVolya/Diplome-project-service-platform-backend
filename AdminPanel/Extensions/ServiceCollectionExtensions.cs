using AdminPanel.Models;
using AdminPanel.Services;
using BLL.Admin.Interfaces;
using BLL.Admin.Services;
using BLL.Public;
using DAL.Context;
using DAL.Seed;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AdminPanel.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Identity на cookie для адмінки. Мобільний API автентифікується через JWT у власному хості;
        /// дві схеми ніде не перетинаються, тому сесії браузера не потрапляють у публічний API.
        /// </summary>
        public static IServiceCollection AddAdminIdentity(this IServiceCollection services)
        {
            services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    options.SignIn.RequireConfirmedEmail = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                // Імʼя користувача має потрапити в cookie, щоб шапка сайту не показувала e-mail.
                .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "ServiceHub.Admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

            return services;
        }

        public static IServiceCollection AddAdminAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AppRoles.StaffPolicy, policy =>
                    policy.RequireRole(AppRoles.StaffRoles));

                options.AddPolicy(AppRoles.AdminOnlyPolicy, policy =>
                    policy.RequireRole(AppRoles.Admin));

                options.AddPolicy(AppRoles.ModerationPolicy, policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.Moderator));

                // Жодна сторінка панелі не є анонімною, якщо явно не вказано протилежне.
                options.FallbackPolicy = options.GetPolicy(AppRoles.StaffPolicy);
            });

            return services;
        }

        public static IServiceCollection AddAdminServices(this IServiceCollection services)
        {
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<IAdminOrderService, AdminOrderService>();
            services.AddScoped<IAdminCategoryService, AdminCategoryService>();
            services.AddScoped<IAdminModerationService, AdminModerationService>();
            services.AddScoped<IAdminPaymentService, AdminPaymentService>();
            services.AddScoped<IAdminChatService, AdminChatService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
            services.AddScoped<INavigationBadgeService, NavigationBadgeService>();

            // Дані для публічної головної сторінки порталу.
            services.AddScoped<IPublicCatalogService, PublicCatalogService>();
            services.AddScoped<ServiceDirectory>();

            // Дані особистого кабінету замовника.
            services.AddScoped<ICabinetService, CabinetService>();

            // Дані кабінету виконавця: власні послуги, портфоліо, календар.
            services.AddScoped<IExecutorCabinetService, ExecutorCabinetService>();

            // Аналітика, фінанси та звернення в підтримку з кабінету виконавця.
            services.AddScoped<IExecutorInsightsService, ExecutorInsightsService>();

            // Коди підтвердження для реєстрації та відновлення пароля.
            // Лежать у памʼяті застосунку, тому екземпляр потрібен один на всіх.
            services.AddSingleton<IVerificationCodes, VerificationCodes>();

            // Поки пошта не налаштована, код іде в журнал. Щоб почати слати листи,
            // достатньо замінити цей рядок реалізацією з SMTP — більше ніде нічого.
            services.AddSingleton<IVerificationCodeSender, LoggingVerificationCodeSender>();

            // Тільки читає файли з wwwroot і кешує результат — один екземпляр на застосунок.
            services.AddSingleton<CatalogPhotos>();

            services.AddMemoryCache();

            // Сервіс без стану: одного екземпляра вистачає на весь застосунок.
            services.AddSingleton<IExcelExportService, ExcelExportService>();

            return services;
        }
    }
}
