using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Application.ServicesMisc;

namespace ViabilityIQ.Web.Extensions
{
    
    /// Master extension method that registers all application services
    
    public static class ServiceIdentityCollectionExtensions
    {
        
        /// Adds all application services to the DI container
        
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // Add database services FIRST (required by Identity)
            services.AddDatabaseServices(configuration);

            // Add identity services SECOND (requires DbContext)
            services.AddIdentityServices(configuration);

            // Add password services - RESETS, ETC
            services.AddScoped<IPasswordService, PasswordService>();

            // Add authentication services THIRD
            services.AddAuthenticationServices(configuration);

            // Add Razor Components
            services.AddRazorComponentsServices();

            // Add logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            // Add health checks
            services.AddHealthChecks();

            return services;
        }
    }
}