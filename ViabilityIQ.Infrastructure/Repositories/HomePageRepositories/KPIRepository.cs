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

        /// <summary>
        /// Get KPI metrics for a specific user
        /// Includes personal metrics and branch-level metrics
        /// Queries tblAssessments and tblBusiness tables
        /// </summary>
        public async Task<KPIMetricsModel> GetKPIMetricsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching KPI metrics for userId: {UserId}", userId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @PreviousMonth INT = MONTH(DATEADD(MONTH, -1, GETUTCDATE()));
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());
                    DECLARE @PreviousYear INT = YEAR(DATEADD(MONTH, -1, GETUTCDATE()));

                    SELECT
                        -- Current Month Metrics
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'InProgress'
                            AND MONTH(CreatedDate) = @CurrentMonth
                            AND YEAR(CreatedDate) = @CurrentYear), 0
                        ) AS ActiveAssessments,
                        
                        -- Active Assessments Trend (vs Previous Month)
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'InProgress'
                            AND MONTH(CreatedDate) = @CurrentMonth
                            AND YEAR(CreatedDate) = @CurrentYear), 0
                        ) - COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'InProgress'
                            AND MONTH(CreatedDate) = @PreviousMonth
                            AND YEAR(CreatedDate) = @PreviousYear), 0
                        ) AS ActiveAssessmentsChange,
                        
                        -- Completed This Month
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'Completed'
                            AND MONTH(CompletedDate) = @CurrentMonth
                            AND YEAR(CompletedDate) = @CurrentYear), 0
                        ) AS CompletedAssessments,
                        
                        -- Completed Trend
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'Completed'
                            AND MONTH(CompletedDate) = @CurrentMonth
                            AND YEAR(CompletedDate) = @CurrentYear), 0
                        ) - COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'Completed'
                            AND MONTH(CompletedDate) = @PreviousMonth
                            AND YEAR(CompletedDate) = @PreviousYear), 0
                        ) AS CompletedAssessmentsChange,
                        
                        -- Pending Reviews (for current user)
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE (ReviewedByUserId = @UserId OR ApprovedByUserId IS NULL)
                            AND Status = 'Pending'), 0
                        ) AS PendingReviews,
                        
                        -- Pending Reviews Trend (today vs yesterday)
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE (ReviewedByUserId = @UserId OR ApprovedByUserId IS NULL)
                            AND Status = 'Pending'
                            AND CAST(ModifiedDate AS DATE) = CAST(GETUTCDATE() AS DATE)), 0
                        ) - COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE (ReviewedByUserId = @UserId OR ApprovedByUserId IS NULL)
                            AND Status = 'Pending'
                            AND CAST(ModifiedDate AS DATE) = CAST(DATEADD(DAY, -1, GETUTCDATE()) AS DATE)), 0
                        ) AS PendingReviewsChange,
                        
                        -- Your Workload (all assessments assigned)
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId), 0
                        ) AS YourWorkload,
                        
                        -- Workload Trend
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId
                            AND MONTH(CreatedDate) = @CurrentMonth
                            AND YEAR(CreatedDate) = @CurrentYear), 0
                        ) AS YourWorkloadChange,
                        
                        -- Total Client Base (unique businesses user is associated with)
                        COALESCE(
                            (SELECT COUNT(DISTINCT b.BusinessId) 
                            FROM tblBusiness b
                            INNER JOIN tblAssessments a ON b.BusinessId = a.BusinessId
                            WHERE a.AssignedToUserId = @UserId OR a.CreatedByUserId = @UserId), 0
                        ) AS TotalClientBase,
                        
                        -- Client Base Trend (YoY)
                        COALESCE(
                            (SELECT COUNT(DISTINCT b.BusinessId) 
                            FROM tblBusiness b
                            INNER JOIN tblAssessments a ON b.BusinessId = a.BusinessId
                            WHERE (a.AssignedToUserId = @UserId OR a.CreatedByUserId = @UserId)
                            AND YEAR(b.CreatedDate) = @CurrentYear), 0
                        ) - COALESCE(
                            (SELECT COUNT(DISTINCT b.BusinessId) 
                            FROM tblBusiness b
                            INNER JOIN tblAssessments a ON b.BusinessId = a.BusinessId
                            WHERE (a.AssignedToUserId = @UserId OR a.CreatedByUserId = @UserId)
                            AND YEAR(b.CreatedDate) = @PreviousYear), 0
                        ) AS TotalClientBaseChange,
                        
                        -- Branch Assessments
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            WHERE a.AssignedToUserId = @UserId
                            AND MONTH(a.CreatedDate) = @CurrentMonth
                            AND YEAR(a.CreatedDate) = @CurrentYear), 0
                        ) AS BranchAssessments,
                        
                        -- Branch Assessments Trend
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            WHERE a.AssignedToUserId = @UserId
                            AND MONTH(a.CreatedDate) = @CurrentMonth
                            AND YEAR(a.CreatedDate) = @CurrentYear), 0
                        ) - COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            WHERE a.AssignedToUserId = @UserId
                            AND MONTH(a.CreatedDate) = @PreviousMonth
                            AND YEAR(a.CreatedDate) = @PreviousYear), 0
                        ) AS BranchAssessmentsChange
                ";

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

        /// <summary>
        /// Get KPI metrics for a specific branch (for managers/admins)
        /// Queries tblAssessments and tblBusiness tables
        /// </summary>
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
                            AND a.Status = 'InProgress'
                            AND MONTH(a.CreatedDate) = @CurrentMonth
                            AND YEAR(a.CreatedDate) = @CurrentYear), 0
                        ) AS ActiveAssessments,
                        
                        -- Trend
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Status = 'InProgress'
                            AND MONTH(a.CreatedDate) = @CurrentMonth
                            AND YEAR(a.CreatedDate) = @CurrentYear), 0
                        ) - COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Status = 'InProgress'
                            AND MONTH(a.CreatedDate) = @PreviousMonth
                            AND YEAR(a.CreatedDate) = @PreviousYear), 0
                        ) AS ActiveAssessmentsChange,
                        
                        -- Completed
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Status = 'Completed'
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