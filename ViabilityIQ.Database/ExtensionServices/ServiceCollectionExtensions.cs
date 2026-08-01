using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;

namespace ViabilityIQ.Application.ExtensionServices
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFinancialCalculationServices(this IServiceCollection services)
        {            
            services.AddSingleton<IFinancialCalculationsEngine, FinancialCalculationsEngine>();

            services.AddScoped<IProjectionStateManager, ProjectionStateManager>();
            services.AddScoped<ICashflowEngine, CashflowEngine>();           

            
            return services;
        }
    }
}
