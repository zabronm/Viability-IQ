using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.HomePageModels;


namespace ViabilityIQ.Infrastructure.Repositories.HomePageRepositories
{
    
    /// Repository for alert data access using Dapper
    
    public class AlertRepository : IAlertRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<AlertRepository> _logger;

        public AlertRepository(IDbConnectionFactory dbConnectionFactory, ILogger<AlertRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

       
        /// Get all alerts for a user (urgent + upcoming)
       
        public async Task<AlertsModel> GetAlertsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching alerts for userId: {UserId}", userId);

                var urgentAssessments = await GetUrgentAssessmentsAsync(userId);
                var upcomingAssessments = await GetUpcomingAssessmentsAsync(userId);

                var alertsModel = new AlertsModel
                {
                    UrgentAssessments = urgentAssessments,
                    DueThisWeek = upcomingAssessments
                };

                _logger.LogInformation("Retrieved {UrgentCount} urgent and {UpcomingCount} upcoming assessments",
                    urgentAssessments.Count, upcomingAssessments.Count);

                return alertsModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alerts");
                return new AlertsModel();
            }
        }

       
        /// Get urgent assessments due within 24 hours
        /// Joins with tblBusiness to get BusinessName
       
        public async Task<List<UrgentAssessmentModel>> GetUrgentAssessmentsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching urgent assessments for userId: {UserId}", userId);

                var query = @"
                    SELECT
                        CONVERT(VARCHAR(50), a.AssessmentId) AS Id,
                        a.AssessmentId,
                        COALESCE(a.CaseNumber, CONCAT('Assessment-', a.AssessmentId)) AS AssessmentName,
                        a.BusinessId,
                        b.BusinessName,
                        a.AssessmentFinishDate AS DueDate,
                        CAST(ROUND(a.ProgressPercentage, 0) AS INT) AS ProgressPercentage,
                        CASE a.StatusId
                            WHEN 1 THEN 'Draft'
                            WHEN 2 THEN 'In Progress'
                            WHEN 3 THEN 'Ready for Review'
                            WHEN 4 THEN 'Completed'
                            WHEN 5 THEN 'Archived'
                            ELSE 'Unknown'
                        END AS Status
                    FROM tblAssessments a
                    INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                    WHERE a.CreatedBy = @UserId
                    AND a.Active = 1
                    AND a.StatusId <> 4
                    AND a.AssessmentFinishDate <= DATEADD(HOUR, 24, GETUTCDATE())
                    AND a.AssessmentFinishDate > GETUTCDATE()
                    ORDER BY a.AssessmentFinishDate ASC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var urgentAssessments = (await connection.QueryAsync<UrgentAssessmentModel>(
                    query,
                    new { UserId = userId }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} urgent assessments", urgentAssessments.Count);
                return urgentAssessments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching urgent assessments");
                return new List<UrgentAssessmentModel>();
            }
        }

       
        /// Get assessments due this week (1-7 days from now)
        /// Joins with tblBusiness to get BusinessName
       
        public async Task<List<UpcomingAssessmentModel>> GetUpcomingAssessmentsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching upcoming assessments for userId: {UserId}", userId);

                var query = @"
                    SELECT
                        CONVERT(VARCHAR(50), a.AssessmentId) AS Id,
                        a.AssessmentId,
                        COALESCE(a.CaseNumber, CONCAT('Assessment-', a.AssessmentId)) AS AssessmentName,
                        a.BusinessId,
                        b.BusinessName,
                        a.AssessmentFinishDate AS DueDate,
                        CAST(ROUND(a.ProgressPercentage, 0) AS INT) AS ProgressPercentage,
                        CASE a.StatusId
                            WHEN 1 THEN 'Draft'
                            WHEN 2 THEN 'In Progress'
                            WHEN 3 THEN 'Ready for Review'
                            WHEN 4 THEN 'Completed'
                            WHEN 5 THEN 'Archived'
                            ELSE 'Unknown'
                        END AS Status
                    FROM tblAssessments a
                    INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                    WHERE a.CreatedBy = @UserId
                    AND a.Active = 1
                    AND a.StatusId <> 4
                    AND a.AssessmentFinishDate > DATEADD(HOUR, 24, GETUTCDATE())
                    AND a.AssessmentFinishDate <= DATEADD(DAY, 7, GETUTCDATE())
                    ORDER BY a.AssessmentFinishDate ASC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var upcomingAssessments = (await connection.QueryAsync<UpcomingAssessmentModel>(
                    query,
                    new { UserId = userId }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} upcoming assessments", upcomingAssessments.Count);
                return upcomingAssessments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching upcoming assessments");
                return new List<UpcomingAssessmentModel>();
            }
        }
    }
}