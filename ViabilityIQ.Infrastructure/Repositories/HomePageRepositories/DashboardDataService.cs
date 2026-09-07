using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Models.Dashboard;

namespace ViabilityIQ.Infrastructure.Repositories.HomePageRepositories
{
    
    /// Service for loading and managing dashboard data using Dapper repositories
    
    public class DashboardDataService : IDashboardDataService
    {
        private readonly ILogger<DashboardDataService> _logger;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IKPIRepository _kpiRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IAnnouncementRepository _announcementRepository;
        private readonly IInsightsRepository _insightsRepository;

        public DashboardDataService(
            ILogger<DashboardDataService> logger,
            IActivityLogRepository activityLogRepository,
            IAssessmentRepository assessmentRepository,
            IKPIRepository kpiRepository,
            IAlertRepository alertRepository,
            IAnnouncementRepository announcementRepository,
            IInsightsRepository insightsRepository)
        {
            _logger = logger;
            _activityLogRepository = activityLogRepository;
            _assessmentRepository = assessmentRepository;
            _kpiRepository = kpiRepository;
            _alertRepository = alertRepository;
            _announcementRepository = announcementRepository;
            _insightsRepository = insightsRepository;
        }

       
        /// Get KPI metrics for the current user
       
        public async Task<KPIMetricsModel> GetKPIMetricsAsync(long userId)
        {
            try
            {
                return await _kpiRepository.GetKPIMetricsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching KPI metrics for userId: {UserId}", userId);
                return new KPIMetricsModel();
            }
        }

       
        /// Get recent activities for the current user
       
        public async Task<List<ActivityLogModel>> GetRecentActivitiesAsync(long userId, int count = 3, string filterType = "all")
        {
            try
            {
                return await _activityLogRepository.GetRecentActivitiesAsync(userId, count, filterType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activities for userId: {UserId}", userId);
                return new List<ActivityLogModel>();
            }
        }

       
        /// Get recent assessments for the current user
       
        public async Task<List<AssessmentModel>> GetRecentAssessmentsAsync(long userId, int count = 5)
        {
            try
            {
                return await _assessmentRepository.GetRecentAssessmentsAsync(userId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent assessments for userId: {UserId}", userId);
                return new List<AssessmentModel>();
            }
        }

       
        /// Get alert data (urgent and upcoming assessments)
       
        public async Task<AlertsModel> GetAlertsAsync(long userId)
        {
            try
            {
                return await _alertRepository.GetAlertsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alerts for userId: {UserId}", userId);
                return new AlertsModel();
            }
        }

       
        /// Get active system announcements (excluding dismissed ones for user)
       
        public async Task<List<SystemAnnouncementModel>> GetSystemAnnouncementsAsync(long userId)
        {
            try
            {
                return await _announcementRepository.GetNonDismissedAnnouncementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system announcements for userId: {UserId}", userId);
                return new List<SystemAnnouncementModel>();
            }
        }

       
        /// Get insights and analytics data
       
        public async Task<InsightsModel> GetInsightsAsync(long userId)
        {
            try
            {
                return await _insightsRepository.GetInsightsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching insights for userId: {UserId}", userId);
                return new InsightsModel();
            }
        }


    }
}
