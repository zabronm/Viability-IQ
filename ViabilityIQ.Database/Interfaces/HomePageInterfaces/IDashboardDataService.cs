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

       
        /// Get KPI metrics for the current user
       
        Task<KPIMetricsModel> GetKPIMetricsAsync(long userId);

       
        /// Get recent activities for the current user
       
        Task<List<ActivityLogModel>> GetRecentActivitiesAsync(long userId, int count = 3, string filterType = "all");

       
        /// Get recent assessments for the current user
       
        Task<List<AssessmentModel>> GetRecentAssessmentsAsync(long userId, int count = 5);

       
        /// Get alert data (urgent and upcoming assessments)
       
        Task<AlertsModel> GetAlertsAsync(long userId);

       
        /// Get active system announcements (excluding dismissed ones)
       
        Task<List<SystemAnnouncementModel>> GetSystemAnnouncementsAsync(long userId);

       
        /// Get insights and analytics data
       
        Task<InsightsModel> GetInsightsAsync(long userId);
    }
}
