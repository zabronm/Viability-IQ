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

        /// <summary>
        /// Get comprehensive insights for a user
        /// Queries tblAssessments and tblBusiness tables
        /// </summary>
        public async Task<InsightsModel> GetInsightsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching insights for userId: {UserId}", userId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());
                    DECLARE @PreviousMonth INT = MONTH(DATEADD(MONTH, -1, GETUTCDATE()));
                    DECLARE @PreviousYear INT = YEAR(DATEADD(MONTH, -1, GETUTCDATE()));

                    DECLARE @TotalAssessments INT = (
                        SELECT COUNT(*) FROM tblAssessments 
                        WHERE AssignedToUserId = @UserId 
                        AND MONTH(CreatedDate) = @CurrentMonth 
                        AND YEAR(CreatedDate) = @CurrentYear
                    );

                    SELECT
                        -- Completion Rate
                        CASE 
                            WHEN @TotalAssessments = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId 
                                AND Status = 'Completed'
                                AND MONTH(CompletedDate) = @CurrentMonth 
                                AND YEAR(CompletedDate) = @CurrentYear) 
                                * 100.0 / @TotalAssessments AS INT
                            )
                        END AS CompletionRatePercent,

                        -- Completion Rate Trend
                        CASE 
                            WHEN (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND MONTH(CreatedDate) = @PreviousMonth 
                            AND YEAR(CreatedDate) = @PreviousYear) = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId 
                                AND Status = 'Completed'
                                AND MONTH(CompletedDate) = @CurrentMonth 
                                AND YEAR(CompletedDate) = @CurrentYear) 
                                * 100.0 / 
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId 
                                AND MONTH(CreatedDate) = @PreviousMonth 
                                AND YEAR(CreatedDate) = @PreviousYear) AS INT
                            ) - 50
                        END AS CompletionRateTrend,

                        -- Average Completion Days (this month)
                        COALESCE(
                            CAST(AVG(DATEDIFF(DAY, a.CreatedDate, a.CompletedDate)) AS INT),
                            0
                        ) AS AverageCompletionDays,

                        -- Previous Average Completion Days
                        COALESCE(
                            (SELECT CAST(AVG(DATEDIFF(DAY, CreatedDate, CompletedDate)) AS INT) 
                            FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'Completed'
                            AND MONTH(CompletedDate) = @PreviousMonth 
                            AND YEAR(CompletedDate) = @PreviousYear),
                            0
                        ) AS PreviousCompletionDays,

                        -- Completion Time Trend
                        COALESCE(
                            CAST(AVG(DATEDIFF(DAY, a.CreatedDate, a.CompletedDate)) AS INT),
                            0
                        ) - COALESCE(
                            (SELECT CAST(AVG(DATEDIFF(DAY, CreatedDate, CompletedDate)) AS INT) 
                            FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'Completed'
                            AND MONTH(CompletedDate) = @PreviousMonth 
                            AND YEAR(CompletedDate) = @PreviousYear),
                            0
                        ) AS CompletionTimeTrend,

                        -- Active Assessments Count
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId AND Status = 'InProgress'), 0
                        ) AS ActiveCount,

                        -- Active Percentage
                        CASE 
                            WHEN (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId AND Status = 'InProgress') 
                                * 100 / (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) AS INT
                            )
                        END AS ActivePercentage,

                        -- Completed Count
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId AND Status = 'Completed'), 0
                        ) AS CompletedCount,

                        -- Completed Percentage
                        CASE 
                            WHEN (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId AND Status = 'Completed') 
                                * 100 / (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) AS INT
                            )
                        END AS CompletedPercentage,

                        -- Pending Count
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId AND Status = 'Pending'), 0
                        ) AS PendingCount,

                        -- Pending Percentage
                        CASE 
                            WHEN (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId AND Status = 'Pending') 
                                * 100 / (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) AS INT
                            )
                        END AS PendingPercentage,

                        -- Other Count (Draft, Archived, etc)
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status NOT IN ('InProgress', 'Completed', 'Pending')), 0
                        ) AS OtherCount,

                        -- Other Percentage
                        CASE 
                            WHEN (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) = 0 THEN 0
                            ELSE CAST(
                                (SELECT COUNT(*) FROM tblAssessments 
                                WHERE AssignedToUserId = @UserId 
                                AND Status NOT IN ('InProgress', 'Completed', 'Pending')) 
                                * 100 / (SELECT COUNT(*) FROM tblAssessments WHERE AssignedToUserId = @UserId) AS INT
                            )
                        END AS OtherPercentage
                    FROM tblAssessments a
                    WHERE a.AssignedToUserId = @UserId 
                    AND a.Status = 'Completed'
                    AND MONTH(a.CompletedDate) = @CurrentMonth 
                    AND YEAR(a.CompletedDate) = @CurrentYear
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var insights = await connection.QueryFirstOrDefaultAsync<InsightsModel>(
                    query,
                    new { UserId = userId }
                );

                // Get top performers
                insights.TopPerformers = (await GetTopPerformersAsync(3)).ToList();

                _logger.LogInformation("Insights retrieved for userId: {UserId}", userId);
                return insights ?? new InsightsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching insights");
                return new InsightsModel();
            }
        }

        /// <summary>
        /// Get insights for a specific branch
        /// Queries tblAssessments and tblBusiness tables
        /// </summary>
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
                                AND a.Status = 'Completed'
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
                            WHERE b.BranchId = @BranchId AND a.Status = 'InProgress'), 0
                        ) AS ActiveCount,
                        0 AS ActivePercentage,
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId AND a.Status = 'Completed'), 0
                        ) AS CompletedCount,
                        0 AS CompletedPercentage,
                        COALESCE(
                            (SELECT COUNT(*) FROM tblAssessments a
                            INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                            WHERE b.BranchId = @BranchId AND a.Status = 'Pending'), 0
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

        /// <summary>
        /// Get top performers leaderboard (current month)
        /// </summary>
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
                    FROM AspNetUsers u
                    INNER JOIN tblAssessments a ON u.Id = a.AssignedToUserId
                    WHERE a.Status = 'Completed'
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