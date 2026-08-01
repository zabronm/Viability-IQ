using ViabilityIQ.Web.ErrorsAndLogging;

namespace ViabilityIQ.Web.Services
{
    public static class ErrorHandlingServiceExtension
    {

        
        /// Adds global error handling and logging services
        
        public static IServiceCollection AddErrorHandlingServices(this IServiceCollection services)
        {
            services.AddTransient<IErrorHandlingService, ErrorHandlingService>();  // ← Changed to AddTransient
            return services;
        }

        
        /// Adds global error handling middleware to the pipeline
        
        public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
            return app;
        }
    }
}
