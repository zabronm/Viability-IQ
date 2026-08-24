
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
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Here you would add your infrastructure services, e.g. repositories, database contexts, etc.

            //services.AddMemoryCache();
            //services.AddScoped<IAppInitializerService, AppInitializerService>();
            services.AddMemoryCache();
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<MasterDataService>();
            services.AddScoped<IExcelEPPlusExportService, ExcelEPPlusExportService>();
            services.AddScoped<IEmailReportingService, EmailReportingService>();
            services.AddScoped<IPdfExportService, PdfExportService>();                                  //Pdf Printing using QuestPDF package
            services.AddScoped(typeof(IGenericDataRepository<>), typeof(GenericDataRepository<>));      //Handles All CRUD using Dapper.Includ          
            services.AddScoped<IDDLookupService, DDLookupService>();                                    //Generic DropDown lookup service
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ReadOnlyRepository<,>));          //Generic Read only
            services.AddScoped<ICashflowRepository, CashflowRepository>();
            services.AddScoped<IDebtorsCreditorsRepository, DebtorsCreditorsRepository>();

            //serices related to home page components
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
            services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
            services.AddScoped<IKPIRepository, KPIRepository>();
            services.AddScoped<IAlertRepository, AlertRepository>();
            services.AddScoped<IAlertDismissalService, AlertDismissalService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IAssessmentRepository, AssessmentRepository>();
            services.AddScoped<IDashboardDataService, DashboardDataService>();
            services.AddScoped<IInsightsRepository, InsightsRepository>();
            services.AddScoped<IUserRepository, UserRepository>();


            //services.AddScoped<IDocumentUploadService,MicrosoftSharePointDocumentService>();        //if using Microsoft SharePoint to store files
            //services.AddScoped<IDocumentUploadService, CloudfareDocumentService>();                 //If using Cloudfare to store files


            return services;
        }
    }
}
