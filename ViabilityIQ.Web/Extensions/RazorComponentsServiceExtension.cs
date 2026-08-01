namespace ViabilityIQ.Web.Extensions
{
    
    /// Extension methods for Razor Components services configuration
    
    public static class RazorComponentsServiceExtension
    {
        
        /// Adds Razor Components services to the DI container
        
        public static IServiceCollection AddRazorComponentsServices(
            this IServiceCollection services)
        {
            // Add Razor Components
            services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add cascading authentication state for child components
            services.AddCascadingAuthenticationState();

            return services;
        }
    }
}