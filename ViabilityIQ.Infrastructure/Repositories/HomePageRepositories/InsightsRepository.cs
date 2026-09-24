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

    /// Repository for insights and analytics data access using Dapper

    public class InsightsRepository : IInsightsRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<InsightsRepository> _logger;

        public InsightsRepository(IDbConnectionFactory dbConnectionFactory, ILogger<InsightsRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

       
        /// Get comprehensive insights for a user
        /// Queries tblAssessments and tblBusiness tables
       
        public async Task<InsightsModel> GetInsightsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching insights for userId: {UserId}", userId);

                var query = @"
                    DECLARE @Total INT = (SELECT COUNT(*) FROM tblAssessments WHERE CreatedBy = @UserId AND Active = 1);
                    DECLARE @CurrentCompleted INT = (SELECT COUNT(*) FROM tblAssessments WHERE CreatedBy = @UserId AND Active = 1 AND StatusId = 4 AND CompletedDate >= DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1));
                    DECLARE @PreviousCompleted INT = (SELECT COUNT(*) FROM tblAssessments WHERE CreatedBy = @UserId AND Active = 1 AND StatusId = 4 AND CompletedDate >= DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1)) AND CompletedDate < DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1));
                    DECLARE @CurrentCreated INT = (SELECT COUNT(*) FROM tblAssessments WHERE CreatedBy = @UserId AND Active = 1 AND CreatedDate >= DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1));
                    DECLARE @PreviousCreated INT = (SELECT COUNT(*) FROM tblAssessments WHERE CreatedBy = @UserId AND Active = 1 AND CreatedDate >= DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1)) AND CreatedDate < DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1));

                    SELECT
                        @Total AS TotalAssessments,
                        CASE WHEN @Total = 0 THEN 0 ELSE CAST(SUM(CASE WHEN StatusId = 4 THEN 1 ELSE 0 END) * 100.0 / @Total AS INT) END AS CompletionRatePercent,
                        (CASE WHEN @CurrentCreated = 0 THEN 0 ELSE CAST(@CurrentCompleted * 100.0 / @CurrentCreated AS INT) END)
                          - (CASE WHEN @PreviousCreated = 0 THEN 0 ELSE CAST(@PreviousCompleted * 100.0 / @PreviousCreated AS INT) END) AS CompletionRateTrend,
                        COALESCE(CAST(AVG(CASE WHEN StatusId = 4 THEN DATEDIFF(DAY, CreatedDate, CompletedDate) END) AS INT), 0) AS AverageCompletionDays,
                        0 AS PreviousCompletionDays,
                        0 AS CompletionTimeTrend,
                        SUM(CASE WHEN StatusId IN (2, 3) THEN 1 ELSE 0 END) AS ActiveCount,
                        CASE WHEN @Total = 0 THEN 0 ELSE CAST(SUM(CASE WHEN StatusId IN (2, 3) THEN 1 ELSE 0 END) * 100.0 / @Total AS INT) END AS ActivePercentage,
                        SUM(CASE WHEN StatusId = 4 THEN 1 ELSE 0 END) AS CompletedCount,
                        CASE WHEN @Total = 0 THEN 0 ELSE CAST(SUM(CASE WHEN StatusId = 4 THEN 1 ELSE 0 END) * 100.0 / @Total AS INT) END AS CompletedPercentage,
                        SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) AS PendingCount,
                        CASE WHEN @Total = 0 THEN 0 ELSE CAST(SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) * 100.0 / @Total AS INT) END AS PendingPercentage,
                        SUM(CASE WHEN StatusId IN (1, 5) THEN 1 ELSE 0 END) AS OtherCount,
                        CASE WHEN @Total = 0 THEN 0 ELSE CAST(SUM(CASE WHEN StatusId IN (1, 5) THEN 1 ELSE 0 END) * 100.0 / @Total AS INT) END AS OtherPercentage,
                        COALESCE(CAST(AVG(CAST(ProgressPercentage AS DECIMAL(10,2))) AS INT), 0) AS AverageReadinessPercent,
                        SUM(CASE WHEN ProgressPercentage >= 100 THEN 1 ELSE 0 END) AS ProjectionReadyCount,
                        SUM(CASE WHEN ProgressPercentage >= 100 AND StatusId <> 4 THEN 1 ELSE 0 END) AS ReadyButIncompleteCount,
                        SUM(CASE WHEN StatusId IN (1, 2) AND ModifiedDate < DATEADD(DAY, -14, GETUTCDATE()) THEN 1 ELSE 0 END) AS StalledCount
                    FROM tblAssessments
                    WHERE CreatedBy = @UserId AND Active = 1;";

                using var connection = _dbConnectionFactory.CreateConnection();
                var insights = await connection.QueryFirstOrDefaultAsync<InsightsModel>(
                    query,
                    new { UserId = userId }
                );

                insights ??= new InsightsModel();
                insights.TopPerformers = (await GetTopPerformersAsync(3)).ToList();

                _logger.LogInformation("Insights retrieved for userId: {UserId}", userId);
                return insights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching insights");
                return new InsightsModel();
            }
        }

       
        /// Get insights for a specific branch
        /// Queries tblAssessments and tblBusiness tables
       
        public async Task<InsightsModel> GetBranchInsightsAsync(int branchId)
        {
            try
            {
                _logger.LogInformation("Fetching insights for branchId: {BranchId}", branchId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());

                    SELECT
                        -- Completion Rate for branch
                        CASE 
                            WHEN (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId 
                            AND MONTH(a.CreatedDate) = @CurrentMonth 
                            AND YEAR(a.CreatedDate) = @CurrentYear) = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments a
                                INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                                WHERE b.BranchId = @BranchId
                                AND a.Active = 1
                                AND a.StatusId = 4
                                AND MONTH(a.CompletedDate) = @CurrentMonth 
                                AND YEAR(a.CompletedDate) = @CurrentYear) 
                                * 100.0 / 
                                (SELECT COUNT(*) FROM tblAssessments a
                                INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                                WHERE b.BranchId = @BranchId 
                                AND MONTH(a.CreatedDate) = @CurrentMonth 
                                AND YEAR(a.CreatedDate) = @CurrentYear) AS INT
                            )
                        END AS CompletionRatePercent,
                        0 AS CompletionRateTrend,
                        0 AS AverageCompletionDays,
                        0 AS PreviousCompletionDays,
                        0 AS CompletionTimeTrend,
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 2), 0
                        ) AS ActiveCount,
                        0 AS ActivePercentage,
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 4), 0
                        ) AS CompletedCount,
                        0 AS CompletedPercentage,
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId
                            AND a.Active = 1
                            AND a.StatusId = 3), 0
                        ) AS PendingCount,
                        0 AS PendingPercentage,
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId 
                            AND a.Status NOT IN ('InProgress', 'Completed', 'Pending')), 0
                        ) AS OtherCount,
                        0 AS OtherPercentage
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var insights = await connection.QueryFirstOrDefaultAsync<InsightsModel>(
                    query,
                    new { BranchId = branchId }
                );

                _logger.LogInformation("Branch insights retrieved for branchId: {BranchId}", branchId);
                return insights ?? new InsightsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch insights");
                return new InsightsModel();
            }
        }

       
        /// Get top performers leaderboard (current month)
       
        public async Task<List<TopPerformerModel>> GetTopPerformersAsync(int count = 10)
        {
            try
            {
                _logger.LogInformation("Fetching top {Count} performers", count);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());

                    SELECT TOP (@Count)
                        u.Id AS UserId,
                        CONCAT(u.FirstName, ' ', u.LastName) AS Name,
                        COUNT(a.AssessmentId) AS CompletedCount,
                        ROW_NUMBER() OVER (ORDER BY COUNT(a.AssessmentId) DESC) AS Rank,
                        0 AS Score
                    FROM tblApplicationUsers u
                    INNER JOIN tblAssessments a ON u.Id = a.CreatedBy
                    WHERE a.StatusId = 4
                    AND MONTH(a.CompletedDate) = @CurrentMonth
                    AND YEAR(a.CompletedDate) = @CurrentYear
                    GROUP BY u.Id, u.FirstName, u.LastName
                    ORDER BY COUNT(a.AssessmentId) DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var topPerformers = (await connection.QueryAsync<TopPerformerModel>(
                    query,
                    new { Count = count }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} top performers", topPerformers.Count);
                return topPerformers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top performers");
                return new List<TopPerformerModel>();
            }
        }
    }
}
