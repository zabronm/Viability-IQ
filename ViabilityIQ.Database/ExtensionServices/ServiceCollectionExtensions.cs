using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;

namespace ViabilityIQ.Application.ExtensionServices
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFinancialCalculationServices(this IServiceCollection services)
        {
            services.AddScoped<IFinancialCalculationsEngine, FinancialCalculationsEngine>();

            return services;
        }
    }
}
