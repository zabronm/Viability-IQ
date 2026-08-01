using Microsoft.EntityFrameworkCore;
using ViabilityIQ.Infrastructure.Data;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Extensions
{
    
    /// Extension methods for database services configuration
    
    public static class DatabaseServiceExtension
    {
        
        /// Adds database services to the DI container
        
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ViabilityIQ_Connection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        "Connection string 'ViabilityIQ_Connection' not found in appsettings.json"
                    );
                }

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("ViabilityIQ.Infrastructure");

                    // Alternative: Using TimeSpan for delay
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    );
                });

                // Enable detailed error messages in development
                if (configuration["Environment"] == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            return services;
        }

        
        /// Applies pending migrations and seeds the database
        /// Does NOT throw - logs errors instead so app can continue running
        
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                var logger = services.GetRequiredService<ILogger<Program>>();
                var toastService = services.GetRequiredService<ToastService>();

                try
                {
                    logger.LogInformation("Starting database initialization...");

                    // Apply pending migrations
                    logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");

                    // Seed initial data if needed
                    logger.LogInformation("Seeding database with initial data...");
                    await SeedDatabaseAsync(context);
                    logger.LogInformation("Database seeded successfully");

                    // Success - show toast
                    toastService.ShowSuccess(
                        "Database initialized successfully",
                        "Database Ready");
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    // Database connection error
                    var errorMessage = $"Database Connection Error: {sqlEx.Message}";
                    logger.LogError(sqlEx, errorMessage);

                    // Show user-friendly error
                    toastService.ShowError(
                        "Unable to connect to the database. Please ensure SQL Server is running and the connection string is correct.",
                        "Database Connection Failed");

                    // Log the full error for debugging
                    logger.LogError(sqlEx, "SQL Server connection failed during database initialization. " +
                        "The application will continue running but database features may not work. " +
                        "Connection string: {ConnectionString}",
                        app.Configuration.GetConnectionString("ViabilityIQ_Connection"));

                    // Don't rethrow - let app continue
                }
                catch (InvalidOperationException ioEx)
                {
                    // DbContext or migration error
                    var errorMessage = $"Database Operation Error: {ioEx.Message}";
                    logger.LogError(ioEx, errorMessage);

                    toastService.ShowError(
                        "A database operation failed. Please check your database configuration.",
                        "Database Error");

                    logger.LogError(ioEx, "Invalid operation during database initialization. " +
                        "This may be due to missing migrations or incorrect DbContext configuration.");

                    // Don't rethrow - let app continue
                }
                catch (Exception ex)
                {
                    // General error
                    var errorMessage = $"Database Initialization Error: {ex.GetType().Name} - {ex.Message}";
                    logger.LogError(ex, errorMessage);

                    toastService.ShowError(
                        "An unexpected database error occurred. Please check the application logs.",
                        "Database Error");

                    logger.LogError(ex, "Unexpected error during database initialization. " +
                        "The application will continue but database features may not work properly.");

                    // Don't rethrow - let app continue
                }
            }
        }

        
        /// Seeds the database with initial data
        
        private static async Task SeedDatabaseAsync(ApplicationDbContext context)
        {
            // Check if database already has data
            if (context.Roles.Any())
            {
                return; // Database already seeded
            }

            // Roles are seeded in DbContext.OnModelCreating()
            // Add additional seeding logic here if needed

            await context.SaveChangesAsync();
        }
    }
}