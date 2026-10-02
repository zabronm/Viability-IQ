using Microsoft.EntityFrameworkCore;
using ViabilityIQ.Infrastructure.Data;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Extensions
{
   
    /// Extension for initializing the database
    /// NOTE: Must be in ViabilityIQ.Web project (not Infrastructure)
   
    public static class DatabaseInitializationExtension
    {
        public static IServiceCollection AddDatabaseStartupAvailability(
            this IServiceCollection services)
        {
            services.AddSingleton<DatabaseStartupState>();
            return services;
        }

        public static async Task<bool> StartWithDatabaseAvailabilityAsync(
            this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting HTTP host before database initialization.");
            await app.StartAsync();

            try
            {
                logger.LogInformation("Initializing database...");
                await app.InitializeDatabaseAsync();
                logger.LogInformation("Database initialized successfully.");
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Database initialization failed. The HTTP host remains online with a service-unavailable page.");
                await app.WaitForShutdownAsync();
                return false;
            }
        }

       
        /// Initializes both IdentityDbContext and ApplicationDbContext
        /// Applies migrations and seeds data
       
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            var maxAttempts = ReadPositiveInt(
                app.Configuration,
                "DatabaseResilience:StartupMaxAttempts",
                5);
            var retryDelaySeconds = ReadPositiveInt(
                app.Configuration,
                "DatabaseResilience:StartupRetryDelaySeconds",
                5);
            var startupState = app.Services.GetRequiredService<DatabaseStartupState>();

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                startupState.MarkInitializing(attempt, maxAttempts);
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    logger.LogInformation(
                        "Starting database initialization attempt {Attempt} of {MaxAttempts}.",
                        attempt,
                        maxAttempts);

                    logger.LogInformation("Applying IdentityDbContext migrations...");
                    var identityContext = services.GetRequiredService<IdentityDbContext>();
                    await identityContext.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
                    logger.LogInformation("IdentityDbContext migrations applied successfully.");

                    logger.LogInformation("Applying ApplicationDbContext migrations...");
                    var appContext = services.GetRequiredService<ApplicationDbContext>();
                    await appContext.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
                    logger.LogInformation("ApplicationDbContext migrations applied successfully.");

                    logger.LogInformation("Database initialization completed successfully.");
                    startupState.MarkReady();
                    return;
                }
                catch (Exception exception) when (
                    attempt < maxAttempts
                    && DatabaseExceptionClassifier.IsTransient(exception))
                {
                    startupState.MarkRetrying(attempt, maxAttempts);
                    logger.LogWarning(
                        exception,
                        "Transient database failure during startup attempt {Attempt} of {MaxAttempts}. Retrying in {RetryDelaySeconds} seconds.",
                        attempt,
                        maxAttempts,
                        retryDelaySeconds);
                    await Task.Delay(
                        TimeSpan.FromSeconds(retryDelaySeconds),
                        app.Lifetime.ApplicationStopping);
                }
                catch (Exception exception)
                {
                    var errorReference = startupState.MarkFailed();
                    logger.LogCritical(
                        exception,
                        "Database initialization failed on attempt {Attempt} of {MaxAttempts}. The database-unavailable page will remain online. ErrorReference: {ErrorReference}",
                        attempt,
                        maxAttempts,
                        errorReference);
                    throw;
                }
            }
        }

        private static int ReadPositiveInt(
            IConfiguration configuration,
            string key,
            int defaultValue) =>
            int.TryParse(configuration[key], out var value) && value > 0
                ? value
                : defaultValue;
    }
}