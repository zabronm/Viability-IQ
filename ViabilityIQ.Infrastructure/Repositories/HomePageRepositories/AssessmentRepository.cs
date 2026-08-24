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
    // <summary>
    /// Repository for assessment data access using Dapper
    
    public class AssessmentRepository : IAssessmentRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<AssessmentRepository> _logger;

        public AssessmentRepository(IDbConnectionFactory dbConnectionFactory, ILogger<AssessmentRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Get recent assessments for a user (latest modified first)
        /// Joins with tblBusiness to get BusinessName
        /// </summary>
        public async Task<List<AssessmentModel>> GetRecentAssessmentsAsync(long userId, int count = 5)
        {
            try
            {
                _logger.LogInformation("Fetching {Count} recent assessments for userId: {UserId}", count, userId);

                var query = @"
                    SELECT TOP (@Count)
                        a.AssessmentId AS Id,
                        a.AssessmentName AS Name,
                        a.BusinessId,
                        b.BusinessName,
                        a.Status,
                        CAST(ROUND(a.ProgressPercentage, 0) AS INT) AS ProgressPercentage,
                        a.ModifiedDate,
                        a.DueDate,
                        '/assessments/' + CAST(a.AssessmentId AS VARCHAR(50)) AS ViewUrl,
                        '/assessments/' + CAST(a.AssessmentId AS VARCHAR(50)) + '/edit' AS EditUrl
                    FROM tblAssessments a
                    INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                    WHERE a.AssignedToUserId = @UserId
                    OR a.CreatedByUserId = @UserId
                    ORDER BY a.ModifiedDate DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var assessments = (await connection.QueryAsync<AssessmentModel>(
                    query,
                    new { UserId = userId, Count = count }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} assessments", assessments.Count);
                return assessments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent assessments");
                return new List<AssessmentModel>();
            }
        }

        /// <summary>
        /// Get assessments filtered by status
        /// </summary>
        public async Task<List<AssessmentModel>> GetAssessmentsByStatusAsync(long userId, string status, int count = 10)
        {
            try
            {
                _logger.LogInformation("Fetching assessments for userId: {UserId} with status: {Status}", userId, status);

                var query = @"
                    SELECT TOP (@Count)
                        a.AssessmentId AS Id,
                        a.AssessmentName AS Name,
                        a.BusinessId,
                        b.BusinessName,
                        a.Status,
                        CAST(ROUND(a.ProgressPercentage, 0) AS INT) AS ProgressPercentage,
                        a.ModifiedDate,
                        a.DueDate,
                        '/assessments/' + CAST(a.AssessmentId AS VARCHAR(50)) AS ViewUrl,
                        '/assessments/' + CAST(a.AssessmentId AS VARCHAR(50)) + '/edit' AS EditUrl
                    FROM tblAssessments a
                    INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                    WHERE (a.AssignedToUserId = @UserId OR a.CreatedByUserId = @UserId)
                    AND a.Status = @Status
                    ORDER BY a.ModifiedDate DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var assessments = (await connection.QueryAsync<AssessmentModel>(
                    query,
                    new { UserId = userId, Status = status, Count = count }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} assessments with status {Status}", assessments.Count, status);
                return assessments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assessments by status");
                return new List<AssessmentModel>();
            }
        }

        /// <summary>
        /// Get a single assessment by ID
        /// </summary>
        public async Task<AssessmentModel> GetAssessmentByIdAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Fetching assessment: {AssessmentId}", assessmentId);

                var query = @"
                    SELECT
                        a.AssessmentId AS Id,
                        a.AssessmentName AS Name,
                        a.BusinessId,
                        b.BusinessName,
                        a.Status,
                        CAST(ROUND(a.ProgressPercentage, 0) AS INT) AS ProgressPercentage,
                        a.ModifiedDate,
                        a.DueDate,
                        '/assessments/' + CAST(a.AssessmentId AS VARCHAR(50)) AS ViewUrl,
                        '/assessments/' + CAST(a.AssessmentId AS VARCHAR(50)) + '/edit' AS EditUrl
                    FROM tblAssessments a
                    INNER JOIN tblBusiness b ON a.BusinessId = b.BusinessId
                    WHERE a.AssessmentId = @AssessmentId
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var assessment = await connection.QueryFirstOrDefaultAsync<AssessmentModel>(
                    query,
                    new { AssessmentId = assessmentId }
                );

                if (assessment != null)
                {
                    _logger.LogInformation("Assessment retrieved: {AssessmentId}", assessmentId);
                }
                else
                {
                    _logger.LogWarning("Assessment not found: {AssessmentId}", assessmentId);
                }

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assessment");
                return null;
            }
        }
    }
}
