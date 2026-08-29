using BLL.Services;
using BLL.Services.Interfaces;
using DAL.Context;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;


namespace API.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}
