using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Models.Dashboard;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{
    
    /// Service interface for loading dashboard data
    
    public interface IDashboardDataService
    {

        /// <summary>
        /// Get KPI metrics for the current user
        /// </summary>
        Task<KPIMetricsModel> GetKPIMetricsAsync(long userId);

        /// <summary>
        /// Get recent activities for the current user
        /// </summary>
        Task<List<ActivityLogModel>> GetRecentActivitiesAsync(long userId, int count = 3, string filterType = "all");

        /// <summary>
        /// Get recent assessments for the current user
        /// </summary>
        Task<List<AssessmentModel>> GetRecentAssessmentsAsync(long userId, int count = 5);

        /// <summary>
        /// Get alert data (urgent and upcoming assessments)
        /// </summary>
        Task<AlertsModel> GetAlertsAsync(long userId);

        /// <summary>
        /// Get active system announcements (excluding dismissed ones)
        /// </summary>
        Task<List<SystemAnnouncementModel>> GetSystemAnnouncementsAsync(long userId);

        /// <summary>
        /// Get insights and analytics data
        /// </summary>
        Task<InsightsModel> GetInsightsAsync(long userId);
    }
}
