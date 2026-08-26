using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebServices(this IServiceCollection services)
        {
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<ToastService>();
            services.AddScoped<ZabOffCanvasService>();
            services.AddScoped<OffCanvasStateService>();
            services.AddScoped<IBusinessHealthAlertService, BusinessHealthAlertService>();
            return services;
        }
    }
}
