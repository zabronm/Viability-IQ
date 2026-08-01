using ViabilityIQ.Web.Components;

namespace ViabilityIQ.Web.Extensions
{
    
    /// Extension methods for configuring the application pipeline
    
    public static class ApplicationBuilderExtensions
    {
        
        /// Configures the HTTP request pipeline
        
        public static WebApplication UseApplicationPipeline(
            this WebApplication app,
            IWebHostEnvironment environment)
        {
            // Configure exception handling
            if (!environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // Use HTTPS redirection
            app.UseHttpsRedirection();

            // Use static files
            app.UseStaticFiles();

            // Use authentication and authorization (REQUIRED ORDER)
            app.UseAuthentication();
            app.UseAuthorization();

            // Use antiforgery
            app.UseAntiforgery();

            // Map Razor Components
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Map health checks
            app.MapHealthChecks("/health");

            return app;
        }
    }
}