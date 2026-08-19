using Microsoft.AspNetCore.Identity;
using ViabilityIQ.Infrastructure.Data;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Extensions
{
    /// <summary>
    /// Extension methods for Identity services configuration
    /// </summary>
    public static class IdentityServiceExtension
    {
        /// <summary>
        /// Adds Identity services to the DI container
        /// </summary>
        public static IServiceCollection AddIdentityServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add Identity with custom options
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Configure password requirements
                ConfigurePasswordOptions(options);

                // Configure lockout settings
                ConfigureLockoutOptions(options);

                // Configure user settings
                ConfigureUserOptions(options);

                // Configure sign-in settings
                ConfigureSignInOptions(options);

                // Configure token settings
                ConfigureTokenOptions(options);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // ============================================================================
            // ENABLE SIGNINMANAGER/USERMANAGER IN RAZOR COMPONENTS
            // ============================================================================
            services.AddScoped<SignInManager<ApplicationUser>>();
            services.AddScoped<UserManager<ApplicationUser>>();
            services.AddScoped<RoleManager<ApplicationRole>>();

            // ============================================================================
            // CRITICAL: Add HttpContextAccessor for reading authentication state
            // ============================================================================
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Configures password policy requirements
        /// </summary>
        private static void ConfigurePasswordOptions(IdentityOptions options)
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 2;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        }

        /// <summary>
        /// Configures account lockout policy
        /// </summary>
        private static void ConfigureLockoutOptions(IdentityOptions options)
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        }

        /// <summary>
        /// Configures user-related settings
        /// </summary>
        private static void ConfigureUserOptions(IdentityOptions options)
        {
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        }

        /// <summary>
        /// Configures sign-in settings
        /// </summary>
        private static void ConfigureSignInOptions(IdentityOptions options)
        {
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.SignIn.RequireConfirmedAccount = false;
        }

        /// <summary>
        /// Configures token-related settings
        /// </summary>
        private static void ConfigureTokenOptions(IdentityOptions options)
        {
            options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.ChangePhoneNumberTokenProvider = TokenOptions.DefaultPhoneProvider;
            options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
        }
    }
}