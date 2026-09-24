using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Infrastructure.Repositories.HomePageRepositories;
using ViabilityIQ.Web.Models.Dashboard;


namespace ViabilityIQ.Infrastructure.Repositories.HomePageRepositories
{

    /// Repository for KPI metrics data access using Dapper    
    public class KPIRepository : IKPIRepository
    {

        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<KPIRepository> _logger;

        public KPIRepository(IDbConnectionFactory dbConnectionFactory, ILogger<KPIRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

       
        /// Get KPI metrics for a specific user
        /// Includes personal metrics and branch-level metrics
        /// Queries tblAssessments and tblBusiness tables
       
        public async Task<KPIMetricsModel> GetKPIMetricsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching KPI metrics for userId: {UserId}", userId);

                var query = @"
                    SELECT
                        COUNT(*) AS TotalAssessments,
                        SUM(CASE WHEN StatusId = 1 THEN 1 ELSE 0 END) AS DraftAssessments,
                        SUM(CASE WHEN StatusId = 2 THEN 1 ELSE 0 END) AS InProgressAssessments,
                        SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) AS ReadyForReviewAssessments,
                        SUM(CASE WHEN ProgressPercentage >= 100 AND StatusId <> 4 THEN 1 ELSE 0 END) AS ProjectionReadyAssessments,
                        SUM(CASE WHEN StatusId = 4 THEN 1 ELSE 0 END) AS CompletedAssessments,
                        SUM(CASE WHEN StatusId IN (2, 3) THEN 1 ELSE 0 END) AS ActiveAssessments,
                        0 AS ActiveAssessmentsChange,
                        0 AS CompletedAssessmentsChange,
                        SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) AS PendingReviews,
                        0 AS PendingReviewsChange,
                        COUNT(*) AS YourWorkload,
                        0 AS YourWorkloadChange,
                        COUNT(DISTINCT BusinessId) AS TotalClientBase,
                        0 AS TotalClientBaseChange,
                        COUNT(*) AS BranchAssessments,
                        0 AS BranchAssessmentsChange
                    FROM tblAssessments
                    WHERE CreatedBy = @UserId AND Active = 1;";

                using var connection = _dbConnectionFactory.CreateConnection();
                var kpiMetrics = await connection.QueryFirstOrDefaultAsync<KPIMetricsModel>(
            query,
            new { UserId = userId }
        );

                _logger.LogInformation("KPI metrics retrieved for userId: {UserId}", userId);
                return kpiMetrics ?? new KPIMetricsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching KPI metrics");
                return new KPIMetricsModel();
            }
        }

       
        /// Get KPI metrics for a specific branch (for managers/admins)
        /// Queries tblAssessments and tblBusiness tables
       
        public async Task<KPIMetricsModel> GetBranchKPIMetricsAsync(int branchId)
        {
            try
            {
                _logger.LogInformation("Fetching KPI metrics for branchId: {BranchId}", branchId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @PreviousMonth INT = MONTH(DATEADD(MONTH, -1, GETUTCDATE()));
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());
                    DECLARE @PreviousYear INT = YEAR(DATEADD(MONTH, -1, GETUTCDATE()));

                    SELECT
                        -- Active Assessments in branch this month
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 2
                            AND MONTH(a.CreatedDate) = @CurrentMonth
                            AND YEAR(a.CreatedDate) = @CurrentYear), 0
                        ) AS ActiveAssessments,
                        
                        -- Trend
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 2
                            AND MONTH(a.CreatedDate) = @CurrentMonth
                            AND YEAR(a.CreatedDate) = @CurrentYear), 0
                        ) - COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 2
                            AND MONTH(a.CreatedDate) = @PreviousMonth
                            AND YEAR(a.CreatedDate) = @PreviousYear), 0
                        ) AS ActiveAssessmentsChange,
                        
                        -- Completed
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 4
                            AND MONTH(a.CompletedDate) = @CurrentMonth
                            AND YEAR(a.CompletedDate) = @CurrentYear), 0
                        ) AS CompletedAssessments,
                        0 AS CompletedAssessmentsChange,
                        0 AS PendingReviews,
                        0 AS PendingReviewsChange,
                        0 AS YourWorkload,
                        0 AS YourWorkloadChange,
                        0 AS TotalClientBase,
                        0 AS TotalClientBaseChange,
                        0 AS BranchAssessments,
                        0 AS BranchAssessmentsChange
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var kpiMetrics = await connection.QueryFirstOrDefaultAsync<KPIMetricsModel>(
            query,
            new { BranchId = branchId }
        );

                _logger.LogInformation("KPI metrics retrieved for branchId: {BranchId}", branchId);
                return kpiMetrics ?? new KPIMetricsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch KPI metrics");
                return new KPIMetricsModel();
            }
        }
    }
}
