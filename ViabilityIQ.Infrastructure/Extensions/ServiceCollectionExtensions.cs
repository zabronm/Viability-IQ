using Microsoft.Extensions.DependencyInjection;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Infrastructure.Reporting;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Infrastructure.Repositories.HomePageRepositories;

namespace ViabilityIQ.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IActivityLogWriter, ActivityLogWriter>();
        services.AddScoped<MasterDataService>();

        services.AddScoped<IExcelEPPlusExportService, ExcelEPPlusExportService>();
        services.AddScoped<IEmailReportingService, EmailReportingService>();
        services.AddScoped<IPdfExportService, PdfExportService>();

        services.AddScoped(typeof(IGenericDataRepository<>), typeof(GenericDataRepository<>));
        services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ReadOnlyRepository<,>));

        services.AddScoped<ICashflowRepository, CashflowRepository>();
        services.AddScoped<IDebtorsCreditorsRepository, DebtorsCreditorsRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();

        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IActivityLogWriter, ActivityLogWriter>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IKPIRepository, KPIRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IInsightsRepository, InsightsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IDDLookupService, DDLookupService>();
        services.AddScoped<IAssessmentDataValidationService, AssessmentDataValidationService>();

        services.AddScoped<IAlertDismissalService, AlertDismissalService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IDashboardDataService, DashboardDataService>();

        return services;
    }
}
