using AdminPanel.Services;
using BLL.Admin.Interfaces;
using BLL.Admin.Services;
using DAL.Context;
using DAL.Seed;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AdminPanel.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Identity на cookie для адмінки. Мобільний API автентифікується через JWT у власному хості;
        // дві схеми ніде не перетинаються, тому сесії браузера не потрапляють у публічний API.
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

            services.AddMemoryCache();

            // Сервіс без стану: одного екземпляра вистачає на весь застосунок.
            services.AddSingleton<IExcelExportService, ExcelExportService>();

            return services;
        }
    }
}
