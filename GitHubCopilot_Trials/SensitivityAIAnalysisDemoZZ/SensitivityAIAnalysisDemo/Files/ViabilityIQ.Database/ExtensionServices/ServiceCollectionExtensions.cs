using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;

namespace ViabilityIQ.Application.ExtensionServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCalculationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IProjectionStateManager, ProjectionStateManager>();

        services.AddScoped<IAssetMovementCalculationService, AssetMovementCalculationService>();
        services.AddScoped<IAssetDepreciationEngine, AssetDepreciationEngine>();
        services.AddScoped<IAssetEngine, AssetEngine>();

        services.AddScoped<IDebtorsCreditorsEngine, DebtorsCreditorsEngine>();
        services.AddScoped<IFinancialCalculationsEngine, FinancialCalculationsEngine>();

        services.AddScoped<IAssessmentVatProjectionService, AssessmentVatProjectionService>();
        services.AddScoped<ICashflowProjectionService, CashflowProjectionService>();
        services.AddScoped<ISensitivityAnalysisService, SensitivityAnalysisService>();
        services.Configure<SensitivityAiOptions>(
            configuration.GetSection(SensitivityAiOptions.SectionName));
        services.AddHttpClient<ISensitivityAiAnalysisService, GeminiSensitivityAiAnalysisService>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<SensitivityAiOptions>>()
                    .Value;
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
            });
        services.AddScoped<ICashflowEngine, CashflowEngine>();

        return services;
    }
}
