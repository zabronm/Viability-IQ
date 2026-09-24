using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;
using ViabilityIQ.Application.Reporting;

namespace ViabilityIQ.Application.ExtensionServices
{
    /// <summary>
    /// Application Layer Service Registration
    /// Handles: Business Logic, Financial Calculations, State Management, Asset Management
    /// 
    /// Registration Order (CRITICAL - Prevents circular dependencies):
    /// 1. IProjectionStateManager (no dependencies on calculation engines)
    /// 2. IAssetMovementCalculationService (asset movement calculations)
    /// 3. IAssetDepreciationEngine (asset depreciation calculations)
    /// 4. IAssetEngine (asset business logic)
    /// 5. IDebtorsCreditorsEngine (debtor/creditor calculations)
    /// 6. IFinancialCalculationsEngine (loan and general financial calculations)
    /// 7. ICashflowEngine (cashflow calculations - can depend on everything above)
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        //public static IServiceCollection AddFinancialCalculationServices(this IServiceCollection services)
        public static IServiceCollection AddFinancialCalculationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ===== STATE MANAGEMENT =====
            // Register FIRST - no dependencies on calculation engines
            services.AddScoped<IProjectionStateManager, ProjectionStateManager>();

            // ===== ASSET SERVICES =====
            // Asset movement calculations (12-month schedules)
            services.AddScoped<IAssetMovementCalculationService, AssetMovementCalculationService>();

            // Asset depreciation aggregations
            services.AddScoped<IAssetDepreciationEngine, AssetDepreciationEngine>();

            // Asset master data and lifecycle management
            services.AddScoped<IAssetEngine, AssetEngine>();

            // ===== FINANCIAL SERVICES =====
            // Debtors and creditors calculations
            services.AddScoped<IDebtorsCreditorsEngine, DebtorsCreditorsEngine>();

            // Loan and general financial calculations (NO IProjectionStateManager dependency)
            services.AddScoped<IFinancialCalculationsEngine, FinancialCalculationsEngine>();

            // Sensitivity analysis calculations (NO IProjectionStateManager dependency)
            services.AddScoped<ISensitivityAnalysisService, SensitivityAnalysisService>();

            // Cashflow calculations (can safely use IProjectionStateManager now)
            services.AddScoped<ICashflowEngine, CashflowEngine>();

            // Debtors/Creditors calculations and Projection Service 
            services.AddScoped<IAccountsProjectionService, AccountsProjectionService>();

            //services.AddScoped<ICashflowProjectionService, CashflowProjectionService>();
            services.AddScoped<IAssessmentVatProjectionService, AssessmentVatProjectionService>();
            services.AddScoped<ICashflowProjectionService, CashflowProjectionService>();

            services.Configure<SensitivityAiOptions>(configuration.GetSection(SensitivityAiOptions.SectionName));


            //============= AI SENSITIVITY ANALYSIS SERVICE =============
            services.AddHttpClient<ISensitivityAiAnalysisService, GeminiSensitivityAiAnalysisService>(
                (serviceProvider, client) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<
                            Microsoft.Extensions.Options.IOptions<SensitivityAiOptions>>()
                        .Value;

                    client.Timeout = TimeSpan.FromSeconds(
                        Math.Clamp(options.TimeoutSeconds, 5, 120));
                })
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError() // Handles HttpRequestException, 5xx server errors, and 408 timeout
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) // Explicitly catch 503
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            //Logger.Warning(
                            //    "Polly retry {RetryAttempt} for Gemini API due to {StatusCode}. Waiting {WaitTime}s...",
                            //    retryAttempt,
                            //    outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message,
                            //    timespan.TotalSeconds);
                        }));


            //=============== REPORTING SERVICES ===============
            services.AddScoped<IAssessmentReportService, AssessmentReportService>();
            services.AddSingleton<IReportWorkbookWriter, OpenXmlReportWorkbookWriter>();



            return services;
        }
    }
}