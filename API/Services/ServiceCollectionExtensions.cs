using BLL.Services;
using BLL.Services.Interfaces;
using DAL.Context;
using DAL.UnitOfWork;
using DAL.UnitOfWork.Interfaces;
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



            services.AddScoped<IJwtService, JwtService>();

            services.AddScoped<IAuthenticationService, AuthenticationService>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IComplaintService, ComplaintService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<IReviewService, ReviewService>();

            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IStatisticService, StatisticService>();
            services.AddScoped<IApplicationService, ApplicationService>();

            services.AddScoped<IPaymentService, PaymentService>();
            services.AddHttpClient<IGoogleMapsService, GoogleMapsService>();
            services.AddScoped<IOrderMessageService, OrderMessageService>();

            return services;
        }
    }
}
