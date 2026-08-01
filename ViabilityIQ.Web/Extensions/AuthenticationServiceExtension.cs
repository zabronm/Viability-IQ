using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Application.ServicesMisc;

namespace ViabilityIQ.Web.Extensions
{
    
    /// Extension methods for authentication services configuration
    
    public static class AuthenticationServiceExtension
    {
        
        /// Adds authentication services to the DI container
        
        public static IServiceCollection AddAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure cookie authentication options
            // Note: AddIdentity already adds the authentication middleware
            // We only need to configure the cookie options here
            services.ConfigureApplicationCookie(options =>
            {
                ConfigureCookieOptions(options, configuration);
            });

            // Add authorization
            services.AddAuthorization();

            // ✅ ADD THIS - CRITICAL FOR BLAZOR SERVER
            // Adds CascadingAuthenticationState to the DI container
            services.AddCascadingAuthenticationState();

            // ✅ ADD THIS - CRITICAL FOR BLAZOR SERVER
            // Adds the ServerAuthenticationStateProvider
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            services.AddScoped<CustomAuthenticationStateProvider>();

            //services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            // Add custom authentication services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();

            return services;
        }

        
        /// Configures cookie authentication options
        
        private static void ConfigureCookieOptions(
            CookieAuthenticationOptions options,
            IConfiguration configuration)
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
        }
    }
}