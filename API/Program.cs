using API.Services;
using BLL.Options;
using BLL.Services;
using BLL.Services.Interfaces;
using DAL.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using API.Validators.Authentication;
using DAL.Configurations;
using System.Threading.Tasks;
using DAL.Seed;
using Microsoft.AspNetCore.Identity;
using Domain.Entities;
using Microsoft.Extensions.Options;


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
            builder.Services.AddEndpointsApiExplorer();

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

            /* ADD SWAGER */
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
            builder.Services.AddValidatorsFromAssemblyContaining<ForgotPasswordRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();


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

            /* ADD REPOSITORIES */

            /* ADD SERVICES */
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


            /* ADD SEADS */
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
