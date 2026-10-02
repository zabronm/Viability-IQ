using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using ViabilityIQ.Application.ExtensionServices;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Extensions;
using ViabilityIQ.Web.Components;
using ViabilityIQ.Web.ErrorsAndLogging;
using ViabilityIQ.Web.Extensions;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("===== APPLICATION STARTUP =====");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting application...");

                try
                {
                    Console.WriteLine("[STEP 1] Creating WebApplicationBuilder...");
                    var builder = WebApplication.CreateBuilder(args);
                    Console.WriteLine("[STEP 1] ✓ Success");

                    // ============================================================================
                    // CONFIGURE LOGGING (SERILOG) - MUST BE FIRST
                    // ============================================================================
                    Console.WriteLine("[STEP 2] Configuring Serilog...");
                    try
                    {
                        LoggingConfiguration.ConfigureSerilog(builder, builder.Configuration);
                        Console.WriteLine("[STEP 2] ✓ Success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 2] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 2] Message: {ex.Message}");
                        Console.WriteLine($"[STEP 2] StackTrace: {ex.StackTrace}");
                        throw;
                    }

                    Log.Information("Serilog initialized successfully");

                    // ============================================================================
                    // ADD RAZOR COMPONENTS
                    // ============================================================================
                    Console.WriteLine("[STEP 3] Adding Razor components...");
                    try
                    {
                        builder.Services
                            .AddRazorComponents()
                            .AddInteractiveServerComponents(options =>
                            {
                                options.DetailedErrors = builder.Environment.IsDevelopment();
                                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                                options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
                                options.MaxBufferedUnacknowledgedRenderBatches = 20;
                            })
                            .AddHubOptions(options =>
                            {
                                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                            });
                        Console.WriteLine("[STEP 3] ✓ Success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 3] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 3] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // ADD CONTROLLERS (For API Endpoints)
                    // ============================================================================
                    Console.WriteLine("[STEP 3.5] Adding controllers...");
                    try
                    {
                        builder.Services.AddControllers();
                        Console.WriteLine("[STEP 3.5] ✓ Controllers added");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 3.5] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 3.5] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // ADD ERROR HANDLING SERVICES
                    // ============================================================================
                    Console.WriteLine("[STEP 4] Adding error handling services...");
                    try
                    {
                        builder.Services.AddErrorHandlingServices();
                        builder.Services.AddDatabaseStartupAvailability();
                        Console.WriteLine("[STEP 4] ✓ Success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 4] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 4] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // ADD WEB SERVICES (ToastService, SessionService, etc.)
                    // ============================================================================
                    Console.WriteLine("[STEP 5] Adding web services...");
                    try
                    {
                        builder.Services.AddWebServices();
                        Console.WriteLine("[STEP 5] ✓ Success - Web services registered");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 5] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 5] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // ADD INFRASTRUCTURE SERVICES (Repositories, Database, etc.)
                    // ============================================================================
                    Console.WriteLine("[STEP 6] Adding infrastructure services...");
                    try
                    {
                        builder.Services.AddInfrastructureServices();
                        Console.WriteLine("[STEP 6] ✓ Success - Repositories and data access registered");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 6] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 6] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // ADD FINANCIAL CALCULATION SERVICES (Business Logic)
                    // ============================================================================
                    Console.WriteLine("[STEP 7] Adding financial calculation services...");
                    try
                    {
                        builder.Services.AddFinancialCalculationServices(builder.Configuration);
                        Console.WriteLine("[STEP 7] ✓ Success - Financial services registered");
                        Console.WriteLine("[STEP 7]   ├─ IProjectionStateManager");
                        Console.WriteLine("[STEP 7]   ├─ IAssetMovementCalculationService");
                        Console.WriteLine("[STEP 7]   ├─ IAssetDepreciationEngine");
                        Console.WriteLine("[STEP 7]   ├─ IAssetEngine");
                        Console.WriteLine("[STEP 7]   ├─ IDebtorsCreditorsEngine");
                        Console.WriteLine("[STEP 7]   ├─ IFinancialCalculationsEngine");
                        Console.WriteLine("[STEP 7]   └─ ICashflowEngine");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 7] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 7] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // ADD APPLICATION SERVICES (Identity, Database, Configuration)
                    // ============================================================================
                    Console.WriteLine("[STEP 8] Adding application services...");
                    try
                    {
                        builder.Services.AddAllApplicationServices(builder.Configuration, builder.Environment);
                        Console.WriteLine("[STEP 8] ✓ Success - Application services registered");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STEP 8] ✗ FAILED: {ex.GetType().Name}");
                        Console.WriteLine($"[STEP 8] Message: {ex.Message}");
                        throw;
                    }

                    // ============================================================================
                    // BUILD APPLICATION
                    // ============================================================================
                    Console.WriteLine("[STEP 9] Building application...");
                    try
                    {
                        var app = builder.Build();
                        Console.WriteLine("[STEP 9] ✓ Success");
                        Log.Information("Application built successfully");

                        // ============================================================================
                        // VERIFY DEPENDENCY RESOLUTION (No circular dependencies)
                        // ============================================================================
                        Console.WriteLine("[STEP 9.5] Verifying dependency resolution...");
                        try
                        {
                            // Create a scope to resolve scoped services properly
                            using (var scope = app.Services.CreateScope())
                            {
                                var projectionManager = scope.ServiceProvider.GetRequiredService<IProjectionStateManager>();
                                var assetMovementService = scope.ServiceProvider.GetRequiredService<IAssetMovementCalculationService>();
                                var assetDepreciationEngine = scope.ServiceProvider.GetRequiredService<IAssetDepreciationEngine>();
                                var assetEngine = scope.ServiceProvider.GetRequiredService<IAssetEngine>();
                                var debtorsEngine = scope.ServiceProvider.GetRequiredService<IDebtorsCreditorsEngine>();
                                var financialEngine = scope.ServiceProvider.GetRequiredService<IFinancialCalculationsEngine>();
                                var cashflowEngine = scope.ServiceProvider.GetRequiredService<ICashflowEngine>();

                                Console.WriteLine("[STEP 9.5] ✓ All services resolved successfully - NO circular dependencies!");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STEP 9.5] ✗ FAILED - Circular dependency or missing service!");
                            Console.WriteLine($"[STEP 9.5] Message: {ex.Message}");
                            throw;
                        }

                        // ============================================================================
                        // CONFIGURE MIDDLEWARE PIPELINE
                        // ============================================================================
                        Console.WriteLine("[STEP 10] Configuring middleware pipeline...");
                        try
                        {
                            app.UseDatabaseStartupStatusPage();
                            // Add global error handling middleware FIRST
                            app.UseGlobalErrorHandling();
                            Console.WriteLine("[STEP 10.1] ✓ Global error handling middleware added");

                            // Configure exception handling
                            if (!app.Environment.IsDevelopment())
                            {
                                app.UseExceptionHandler("/error");
                                app.UseHsts();
                            }
                            else
                            {
                                app.UseDeveloperExceptionPage();
                            }

                            Console.WriteLine("[STEP 10] ✓ Middleware configured");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STEP 10] ✗ FAILED: {ex.GetType().Name}");
                            Console.WriteLine($"[STEP 10] Message: {ex.Message}");
                            throw;
                        }

                        // ============================================================================
                        // CONFIGURE APPLICATION PIPELINE
                        // ============================================================================
                        Console.WriteLine("[STEP 11] Configuring application pipeline...");
                        try
                        {
                            app.UseApplicationPipeline(app.Environment);
                            app.MapControllers();
                            Console.WriteLine("[STEP 11] ✓ Application pipeline configured");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STEP 11] ✗ FAILED: {ex.GetType().Name}");
                            Console.WriteLine($"[STEP 11] Message: {ex.Message}");
                            throw;
                        }

                        if (!await app.StartWithDatabaseAvailabilityAsync())
                            return;

                        // ============================================================================
                        // CONFIGURE FOR DEPLOYMENT
                        // ============================================================================
                        Console.WriteLine("[STEP 13] Configuring for deployment...");
                        try
                        {
                            app.ConfigureForDeployment();
                            Console.WriteLine("[STEP 13] ✓ Deployment configuration complete");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STEP 13] ✗ FAILED: {ex.GetType().Name}");
                            Console.WriteLine($"[STEP 13] Message: {ex.Message}");
                            throw;
                        }

                        // ============================================================================
                        // PRINT DEPLOYMENT CHECKLIST
                        // ============================================================================
                        Console.WriteLine("[STEP 14] Printing deployment checklist...");
                        try
                        {
                            app.PrintDeploymentChecklist();
                            Console.WriteLine("[STEP 14] ✓ Deployment checklist printed");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STEP 14] ⚠ Could not print checklist: {ex.Message}");
                            Log.Warning(ex, "Failed to print deployment checklist");
                        }

                        // ============================================================================
                        // GENERATE DEPLOYMENT REPORT
                        // ============================================================================
                        Console.WriteLine("[STEP 15] Generating deployment report...");
                        try
                        {
                            app.GenerateDeploymentReport();
                            Console.WriteLine("[STEP 15] ✓ Deployment report generated");
                            Log.Information("Deployment report generated successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STEP 15] ⚠ Could not generate report: {ex.Message}");
                            Log.Warning(ex, "Failed to generate deployment report");
                        }

                        // ============================================================================
                        // START APPLICATION
                        // ============================================================================
                        Console.WriteLine("[STEP 16] Starting application...");
                        Log.Information("Application started successfully");
                        await app.WaitForShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n===== EXCEPTION DETAILS =====");
                        Console.WriteLine($"Type: {ex.GetType().FullName}");
                        Console.WriteLine($"Message: {ex.Message}");
                        Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");

                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"\n===== INNER EXCEPTION =====");
                            Console.WriteLine($"Type: {ex.InnerException.GetType().FullName}");
                            Console.WriteLine($"Message: {ex.InnerException.Message}");
                            Console.WriteLine($"\nStack Trace:\n{ex.InnerException.StackTrace}");
                        }

                        Log.Fatal(ex, "Application terminated unexpectedly");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n===== UNHANDLED FATAL ERROR =====");
                    Console.WriteLine($"Type: {ex.GetType().FullName}");
                    Console.WriteLine($"Message: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    try
                    {
                        Log.Fatal(ex, "Application crashed during startup");
                    }
                    catch { }

                    Environment.Exit(1);
                }
            }
            finally
            {
                Console.WriteLine("\n===== SHUTTING DOWN =====");
                Log.CloseAndFlush();
                Console.WriteLine("Application shut down");
            }
        }
    }
}