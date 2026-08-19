using Microsoft.AspNetCore.Builder;
using Serilog;

namespace ViabilityIQ.Web.Extensions
{
    /// <summary>
    /// Publishing and deployment-related extensions
    /// Handles configuration validation, security setup, and reporting for production/staging
    /// </summary>
    public static class PublishingExtensions
    {
        /// <summary>
        /// Configure application for production/staging deployment
        /// Validates all required configuration and applies security settings
        /// </summary>
        public static void ConfigureForDeployment(this WebApplication app)
        {
            Console.WriteLine("\n[DEPLOYMENT] Configuring for deployment...");

            try
            {
                var environment = app.Environment;
                var config = app.Services.GetRequiredService<IConfiguration>();

                // Log deployment environment
                LogDeploymentEnvironment(app, config);

                // Validate deployment configuration
                ValidateDeploymentConfiguration(config, environment);

                // Configure HTTPS/HSTS for production
                if (environment.IsProduction())
                {
                    ConfigureProductionSecurity(app);
                }
                else if (environment.IsEnvironment("Staging"))
                {
                    ConfigureStagingSecurity(app);
                }

                Console.WriteLine("[DEPLOYMENT] ✓ Deployment configuration complete\n");
                Log.Information("Application configured for deployment");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEPLOYMENT] ✗ FAILED: {ex.Message}");
                Log.Error(ex, "Deployment configuration failed");
                throw;
            }
        }

        /// <summary>
        /// Print deployment checklist to console
        /// Shows if all required settings are configured
        /// </summary>
        public static void PrintDeploymentChecklist(this WebApplication app)
        {
            var config = app.Services.GetRequiredService<IConfiguration>();
            var environment = app.Environment;

            Console.WriteLine("\n====================================");
            Console.WriteLine("DEPLOYMENT CHECKLIST");
            Console.WriteLine("====================================");

            var checks = new List<(string Name, bool Passed)>
            {
                ("Environment Set", !string.IsNullOrEmpty(environment.EnvironmentName)),
                ("Connection String Configured", !string.IsNullOrEmpty(config.GetConnectionString("ViabilityIQ_Connection"))),
                ("JWT Configured", !string.IsNullOrEmpty(config["Jwt:Key"])),
                ("Serilog Configured", config.GetSection("Serilog").Exists()),
                ("Authentication Configured", config.GetSection("Authentication").Exists()),
                ("AllowedHosts Set", !string.IsNullOrEmpty(config["AllowedHosts"])),
            };

            if (!environment.IsDevelopment())
            {
                checks.Add(("HTTPS Enforced", environment.IsProduction()));
                checks.Add(("DetailedErrors Disabled", !config.GetValue<bool>("DetailedErrors", false)));
            }

            foreach (var (name, passed) in checks)
            {
                var symbol = passed ? "✓" : "✗";
                var status = passed ? "" : " [WARNING]";
                Console.WriteLine($"  {symbol} {name}{status}");
            }

            Console.WriteLine("====================================\n");

            Log.Information("Deployment checklist: {@Checklist}", checks);
        }

        /// <summary>
        /// Generate detailed deployment report
        /// Shows environment, configuration sources, security, database, authentication, and logging settings
        /// </summary>
        public static void GenerateDeploymentReport(this WebApplication app)
        {
            var config = app.Services.GetRequiredService<IConfiguration>();
            var environment = app.Environment;

            Console.WriteLine("\n====================================");
            Console.WriteLine("DEPLOYMENT REPORT");
            Console.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("====================================\n");

            Console.WriteLine("ENVIRONMENT:");
            Console.WriteLine($"  Name: {environment.EnvironmentName}");
            Console.WriteLine($"  ContentRoot: {environment.ContentRootPath}");
            Console.WriteLine($"  WebRoot: {environment.WebRootPath}");

            Console.WriteLine("\nCONFIGURATION SOURCES:");
            Console.WriteLine("  • appsettings.json");
            Console.WriteLine($"  • appsettings.{environment.EnvironmentName}.json");
            Console.WriteLine("  • Environment Variables");

            Console.WriteLine("\nSECURITY:");
            Console.WriteLine($"  HTTPS Required: {!environment.IsDevelopment()}");
            Console.WriteLine($"  Detailed Errors: {config.GetValue<bool>("DetailedErrors", false)}");
            Console.WriteLine($"  Allowed Hosts: {config["AllowedHosts"]}");

            Console.WriteLine("\nDATABASE:");
            var connString = config.GetConnectionString("ViabilityIQ_Connection");
            Console.WriteLine($"  Connection String: {MaskConnectionString(connString)}");

            Console.WriteLine("\nAUTHENTICATION:");
            Console.WriteLine($"  Cookie Name: {config["Authentication:CookieName"]}");
            Console.WriteLine($"  JWT Issuer: {config["Jwt:Issuer"]}");
            Console.WriteLine($"  JWT Audience: {config["Jwt:Audience"]}");

            Console.WriteLine("\nLOGGING:");
            Console.WriteLine($"  Serilog Configured: {config.GetSection("Serilog").Exists()}");
            var logLevel = config["Logging:LogLevel:Default"];
            Console.WriteLine($"  Log Level: {logLevel}");

            Console.WriteLine("\n====================================\n");

            Log.Information("Deployment report generated for {@Environment}", new
            {
                Environment = environment.EnvironmentName,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Log deployment environment information
        /// </summary>
        private static void LogDeploymentEnvironment(WebApplication app, IConfiguration config)
        {
            Console.WriteLine("[DEPLOYMENT] Environment Information:");
            Console.WriteLine($"  • Environment: {app.Environment.EnvironmentName}");
            Console.WriteLine($"  • Is Production: {app.Environment.IsProduction()}");
            Console.WriteLine($"  • Is Staging: {app.Environment.IsEnvironment("Staging")}");
            Console.WriteLine($"  • Is Development: {app.Environment.IsDevelopment()}");

            var connString = config.GetConnectionString("ViabilityIQ_Connection");
            if (!string.IsNullOrEmpty(connString))
            {
                Console.WriteLine($"  • Database: {MaskConnectionString(connString)}");
            }

            var host = config["AllowedHosts"];
            if (!string.IsNullOrEmpty(host))
            {
                Console.WriteLine($"  • Allowed Hosts: {host}");
            }

            Log.Information("Deployment Environment: {@DeploymentInfo}", new
            {
                Environment = app.Environment.EnvironmentName,
                IsProduction = app.Environment.IsProduction(),
                IsStaging = app.Environment.IsEnvironment("Staging"),
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Validate deployment configuration
        /// Throws exceptions if critical settings are missing
        /// </summary>
        private static void ValidateDeploymentConfiguration(IConfiguration config, IWebHostEnvironment environment)
        {
            Console.WriteLine("[DEPLOYMENT] Validating configuration...");

            // Check connection string
            var connString = config.GetConnectionString("ViabilityIQ_Connection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ViabilityIQ_Connection' is not configured. " +
                    "Ensure appsettings.{Environment}.json exists with proper connection string.");
            }
            Console.WriteLine("  ✓ Connection String: Valid");

            // Check JWT configuration for non-development environments
            if (!environment.IsDevelopment())
            {
                var jwtKey = config["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    throw new InvalidOperationException(
                        $"JWT Key not configured for {environment.EnvironmentName} environment. " +
                        "Set 'Jwt:Key' in appsettings.{Environment}.json or environment variable 'Jwt__Key'");
                }

                if (jwtKey.Length < 32)
                {
                    throw new InvalidOperationException(
                        $"JWT Key is too short ({jwtKey.Length} chars). Minimum 32 characters required for production/staging.");
                }

                Console.WriteLine($"  ✓ JWT Key: Configured ({jwtKey.Length} chars)");
            }

            // Check CORS/AllowedHosts
            var allowedHosts = config["AllowedHosts"];
            if (string.IsNullOrEmpty(allowedHosts) || allowedHosts == "*")
            {
                Console.WriteLine("  ⚠ AllowedHosts: Set to allow all (*)");
                Log.Warning("AllowedHosts is set to '*' - consider restricting for production");
            }
            else
            {
                Console.WriteLine($"  ✓ AllowedHosts: {allowedHosts}");
            }

            Console.WriteLine("[DEPLOYMENT] ✓ Configuration validation passed");
        }

        /// <summary>
        /// Configure security headers and HTTPS for production
        /// </summary>
        private static void ConfigureProductionSecurity(WebApplication app)
        {
            Console.WriteLine("[DEPLOYMENT] Configuring production security...");

            // HSTS (HTTP Strict Transport Security)
            app.UseHsts();
            Console.WriteLine("  ✓ HSTS enabled");

            // Add security headers middleware
            app.Use(async (context, next) =>
            {
                // Prevent clickjacking
                context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

                // Prevent MIME type sniffing
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

                // Enable XSS protection
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

                // Content Security Policy - Allow jsDelivr CDN for styles, scripts, and fonts
                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                    "font-src 'self' https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https://cdn.jsdelivr.net");

                await next();
            });
            Console.WriteLine("  ✓ Security headers added (jsDelivr CDN allowed)");

            Log.Information("Production security configured - HSTS and security headers enabled");
        }

        /// <summary>
        /// Configure security for staging environment
        /// </summary>
        private static void ConfigureStagingSecurity(WebApplication app)
        {
            Console.WriteLine("[DEPLOYMENT] Configuring staging security...");

            app.UseHsts();
            Console.WriteLine("  ✓ HSTS enabled");

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

                // Add CSP with CDN support
                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                    "font-src 'self' https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https://cdn.jsdelivr.net");

                await next();
            });
            Console.WriteLine("  ✓ Basic security headers added (jsDelivr CDN allowed)");

            Log.Information("Staging security configured");
        }

        /// <summary>
        /// Mask connection string for safe logging
        /// </summary>
        private static string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "(Not configured)";

            if (connectionString.Length <= 20)
                return "***MASKED***";

            return $"{connectionString.Substring(0, 10)}...***...{connectionString.Substring(connectionString.Length - 10)}";
        }
    }
}