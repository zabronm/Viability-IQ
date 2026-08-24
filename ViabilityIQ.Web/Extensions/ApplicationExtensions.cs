// FILE: ApplicationExtensions.cs (NEW - Put in ViabilityIQ.Web.Extensions folder)

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Application.ServicesMisc;
using ViabilityIQ.Infrastructure.Data;
using ViabilityIQ.Infrastructure.Extensions;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Extensions
{
    /// <summary>
    /// Master application extensions - ALL services registered here
    /// Single entry point to avoid naming conflicts and ambiguity
    /// </summary>
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddAllApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // ============================================================================
            // 1. DATABASE CONTEXTS
            // ============================================================================
            var connectionString = configuration.GetConnectionString("ViabilityIQ_Connection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ViabilityIQ_Connection' not found in appsettings.json");
            }

            // Identity DbContext
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("ViabilityIQ.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                }));

            // Application DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("ViabilityIQ.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                }));

            // ============================================================================
            // 2. IDENTITY
            // ============================================================================
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 2;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedAccount = false;

                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.ChangePhoneNumberTokenProvider = TokenOptions.DefaultPhoneProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<SignInManager<ApplicationUser>>();
            services.AddScoped<UserManager<ApplicationUser>>();
            services.AddScoped<RoleManager<ApplicationRole>>();

            // ============================================================================
            // 3. AUTHENTICATION
            // ============================================================================
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/access-denied";
                options.Cookie.Name = "ViabilityIQ.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
            });

            services.AddHttpClient<AuthenticationService>()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                });

            services.AddHttpClient();
            services.AddAuthorization();
            services.AddCascadingAuthenticationState();
            services.AddScoped<CustomAuthenticationStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<CustomAuthenticationStateProvider>());

            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IUserService, UserService>();

            services.AddHttpContextAccessor();

            // ============================================================================
            // 4. INFRASTRUCTURE SERVICES (Dapper repositories, etc.)
            // ============================================================================
            services.AddInfrastructureServices();

            // ============================================================================
            // 5. LOGGING
            // ============================================================================
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            // ============================================================================
            // 6. HEALTH CHECKS
            // ============================================================================
            services.AddHealthChecks();

            return services;
        }
    }
}