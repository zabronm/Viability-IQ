using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using ViabilityIQ.Infrastructure.Data;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Extensions
{
    /// <summary>
    /// Extension for initializing the database
    /// NOTE: Must be in ViabilityIQ.Web project (not Infrastructure)
    /// </summary>
    public static class DatabaseInitializationExtension
    {
        /// <summary>
        /// Initializes both IdentityDbContext and ApplicationDbContext
        /// Applies migrations and seeds data
        /// </summary>
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                var toastService = services.GetRequiredService<ToastService>();

                try
                {
                    logger.LogInformation("Starting database initialization...");

                    // ✅ Migrate IdentityDbContext
                    logger.LogInformation("Applying IdentityDbContext migrations...");
                    var identityContext = services.GetRequiredService<IdentityDbContext>();
                    await identityContext.Database.MigrateAsync();
                    logger.LogInformation("✓ IdentityDbContext migrations applied successfully");

                    // ✅ Migrate ApplicationDbContext
                    logger.LogInformation("Applying ApplicationDbContext migrations...");
                    var appContext = services.GetRequiredService<ApplicationDbContext>();
                    await appContext.Database.MigrateAsync();
                    logger.LogInformation("✓ ApplicationDbContext migrations applied successfully");

                    logger.LogInformation("✓✓✓ Database initialization completed successfully");
                    toastService.ShowSuccess(
                        "Database initialized successfully",
                        "Database Ready");
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    logger.LogError(sqlEx, "SQL Server connection error during database initialization");
                    toastService.ShowError(
                        "Unable to connect to the database. Please ensure SQL Server is running and the connection string is correct.",
                        "Database Connection Failed");
                }
                catch (InvalidOperationException ioEx)
                {
                    logger.LogError(ioEx, "Invalid operation during database initialization");
                    toastService.ShowError(
                        "A database operation failed. Please check your database configuration.",
                        "Database Error");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error during database initialization: {Message}", ex.Message);
                    toastService.ShowError(
                        "An unexpected database error occurred. Please check the application logs.",
                        "Database Error");
                }
            }
        }
    }
}