using Microsoft.Extensions.DependencyInjection;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;
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
        public static IServiceCollection AddFinancialCalculationServices(this IServiceCollection services)
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

            // Cashflow calculations (can safely use IProjectionStateManager now)
            services.AddScoped<ICashflowEngine, CashflowEngine>();

            return services;
        }
    }
}