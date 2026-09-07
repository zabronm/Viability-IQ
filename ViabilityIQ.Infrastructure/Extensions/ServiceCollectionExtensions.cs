using Microsoft.Extensions.DependencyInjection;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Infrastructure.Reporting;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Infrastructure.Repositories.HomePageRepositories;

namespace ViabilityIQ.Infrastructure.Extensions
{
    /// <summary>
    /// Infrastructure Layer Service Registration
    /// Handles: Database, Repositories, Data Access, External Services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // ===== MEMORY CACHING =====
            services.AddMemoryCache();

            // ===== DATABASE FACTORY =====
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

            // ===== MASTER DATA SERVICE =====
            services.AddScoped<MasterDataService>();

            // ===== EXPORT & REPORTING SERVICES =====
            services.AddScoped<IExcelEPPlusExportService, ExcelEPPlusExportService>();
            services.AddScoped<IEmailReportingService, EmailReportingService>();
            services.AddScoped<IPdfExportService, PdfExportService>();

            // ===== GENERIC REPOSITORIES =====
            services.AddScoped(typeof(IGenericDataRepository<>), typeof(GenericDataRepository<>));
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ReadOnlyRepository<,>));

            // ===== SPECIALIZED REPOSITORIES =====
            services.AddScoped<ICashflowRepository, CashflowRepository>();
            services.AddScoped<IDebtorsCreditorsRepository, DebtorsCreditorsRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();

            // ===== HOME PAGE REPOSITORIES =====
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
            services.AddScoped<IKPIRepository, KPIRepository>();
            services.AddScoped<IAlertRepository, AlertRepository>();
            services.AddScoped<IAssessmentRepository, AssessmentRepository>();
            services.AddScoped<IInsightsRepository, InsightsRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // ===== LOOKUP & VALIDATION SERVICES =====
            services.AddScoped<IDDLookupService, DDLookupService>();
            services.AddScoped<IAssessmentDataValidationService, AssessmentDataValidationService>();

            // ===== HOME PAGE SERVICES =====
            services.AddScoped<IAlertDismissalService, AlertDismissalService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IDashboardDataService, DashboardDataService>();

            //services.AddScoped<IDocumentUploadService, MicrosoftSharePointDocumentService>();  // Microsoft SharePoint
            //services.AddScoped<IDocumentUploadService, CloudfareDocumentService>();           // Cloudflare

            return services;
        }
    }
}
