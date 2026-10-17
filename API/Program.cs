using API.DTO.Category;
using API.DTO.Payment;
using API.Middleware;
using API.Services;
using API.Swagger;
using API.Validators.Admin;
using API.Validators.Authentication;
using API.Validators.Category;
using API.Validators.Complaint;
using API.Validators.Notification;
using API.Validators.Order;
using API.Validators.OrderMessage;
using API.Validators.Payment;
using API.Validators.Review;
using API.Validators.Statistic;
using BLL.Options;
using BLL.Services;
using BLL.Services.Interfaces;
using BLL.Services.PaymentGateways;
using DAL.Configurations;
using DAL.Context;
using DAL.Seed;
using Domain.Entities;
using FirebaseAdmin;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.Tasks;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /* CONNECT DATABASE */
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(connectionString)
                );

            builder.Services.AddControllers();

            /* GLOBAL ERROR HANDLING */
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddEndpointsApiExplorer();

            var firebaseKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-key.json");
            if (File.Exists(firebaseKeyPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(firebaseKeyPath)
                });
            }

            /* ADD AUTHENTICATION */
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                builder.Configuration["Jwt:Key"]!
                            )
                        ),

                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            Console.WriteLine(
                                $"JWT received: {context.Token != null}"
                            );

                            return Task.CompletedTask;
                        },

                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("JWT VALIDATED");
                            Console.WriteLine($"User: {context.Principal?.Identity?.Name}");

                            return Task.CompletedTask;
                        },

                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("JWT AUTHENTICATION FAILED");
                            Console.WriteLine(context.Exception);

                            return Task.CompletedTask;
                        },

                        OnChallenge = context =>
                        {
                            Console.WriteLine("JWT CHALLENGE");
                            Console.WriteLine($"Error: {context.Error}");
                            Console.WriteLine($"Description: {context.ErrorDescription}");

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            /* PAYMENT GATEWAYS & OPTIONS */
            builder.Services.AddOptions<PaymentOptions>()
                .Bind(builder.Configuration.GetSection(PaymentOptions.SectionName))
                .Validate(o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _), "Payments:PublicBaseUrl must be an absolute URL.")
                .Validate(o => Uri.TryCreate(o.ReturnUrl, UriKind.Absolute, out _), "Payments:ReturnUrl must be an absolute URL.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Currency), "Payments:Currency is required.")
                .Validate(
                    o => builder.Environment.IsDevelopment() ||
                           (!o.Development.MapUnreferencedTestEvents && !o.Development.SkipAmountCheckForTestEvents),
                    "Payments:Development options may only be enabled in the Development environment.")
                .ValidateOnStart();

            builder.Services.AddOptions<StripeOptions>()
                .Bind(builder.Configuration.GetSection(StripeOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey), "Stripe:SecretKey is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.WebhookSecret), "Stripe:WebhookSecret is required.")
                .ValidateOnStart();

            builder.Services.AddOptions<LiqPayOptions>()
                .Bind(builder.Configuration.GetSection(LiqPayOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.PublicKey), "LiqPay:PublicKey is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.PrivateKey), "LiqPay:PrivateKey is required.")
                .ValidateOnStart();

            builder.Services.AddOptions<WayForPayOptions>()
                .Bind(builder.Configuration.GetSection(WayForPayOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.MerchantAccount), "WayForPay:MerchantAccount is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.MerchantSecretKey), "WayForPay:MerchantSecretKey is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.DomainName), "WayForPay:DomainName is required.")
                .ValidateOnStart();

            // Gateways are stateless, so singletons are fine.
            builder.Services.AddSingleton<IPaymentGateway, LiqPayGateway>();
            builder.Services.AddSingleton<IPaymentGateway, WayForPayGateway>();
            builder.Services.AddSingleton<IPaymentGateway, StripeGateway>();

            builder.Services.AddSingleton<PaymentGatewayFactory>();

            /* ADD SWAGGER */
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Bearer {token}"
                    });

                // Makes Swagger UI send real webhook test requests (form fields / JSON body).
                options.OperationFilter<WebhookRequestBodyOperationFilter>();

                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement {
                            {
                            new OpenApiSecurityScheme
                                {
                                    Reference =
                                        new OpenApiReference
                                        {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                        }
                                },
                            Array.Empty<string>()
                            }
                    });
            });

            /* ADD AUTOMAPPER FOR DTO */
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            /* ADD FLUENT VALIDATORS */
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<AddAdminRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ForgotPasswordRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateCategoryValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateCategoryValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateComplaintValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateComplaintStatusValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateMassNotificationValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateOrderValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<SendOrderMessageValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreatePaymentValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<PaymentCallbackValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateReviewValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateReviewValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<DateRangeValidator>();

            /* ADD OPTIONS */
            builder.Services.Configure<EmailOptions>(
                builder.Configuration.GetSection("Email"));

            builder.Services.AddOptions<AdminSeedOptions>()
                .Bind(builder.Configuration.GetSection(AdminSeedOptions.SectionName))
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Email),
                    "AdminSeed:Email must be provided in configuration.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Password),
                    "AdminSeed:Password must be provided in configuration.")
                .ValidateOnStart();

            /* ADD REPOSITORIES & SERVICES */
            builder.Services.AddApplicationServices();

            builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            var app = builder.Build();

            // Request body buffering for webhooks
            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next();
            });

            // Must be the first middleware so it can catch exceptions from everything below.
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.Use(async (ctx, next) =>
            {
                Console.WriteLine($"{ctx.Request.Method} {ctx.Request.Path}");
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            /* ADD SEEDS */
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var adminOptions = services.GetRequiredService<IOptions<AdminSeedOptions>>();

                await roleManager.SeedRolesAsync();
                await userManager.SeedSuperAdminAsync(adminOptions);
            }

            app.Run();
        }
    }
}