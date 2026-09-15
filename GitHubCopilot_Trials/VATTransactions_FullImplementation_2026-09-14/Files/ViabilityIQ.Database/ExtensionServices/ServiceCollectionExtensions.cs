using Microsoft.Extensions.DependencyInjection;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;

namespace ViabilityIQ.Application.ExtensionServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCalculationServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectionStateManager, ProjectionStateManager>();

        services.AddScoped<IAssetMovementCalculationService, AssetMovementCalculationService>();
        services.AddScoped<IAssetDepreciationEngine, AssetDepreciationEngine>();
        services.AddScoped<IAssetEngine, AssetEngine>();

        services.AddScoped<IDebtorsCreditorsEngine, DebtorsCreditorsEngine>();
        services.AddScoped<IFinancialCalculationsEngine, FinancialCalculationsEngine>();

        services.AddScoped<IAssessmentVatProjectionService, AssessmentVatProjectionService>();
        services.AddScoped<ICashflowProjectionService, CashflowProjectionService>();
        services.AddScoped<ICashflowEngine, CashflowEngine>();

        return services;
    }
}
