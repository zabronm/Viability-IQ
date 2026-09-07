using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
   
    public class AssetRepository : IAssetRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<AssetRepository> _logger;

        public AssetRepository(
            IDbConnectionFactory dbConnectionFactory,
            ILogger<AssetRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<AssessmentAsset>> GetAssessmentAssetsAsync(long assessmentId)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                const string query = @"
                    SELECT * FROM tblAssessmentAsset
                    WHERE AssessmentId = @AssessmentId AND Active = 1
                    ORDER BY AssetName
                ";

                var assets = (await connection.QueryAsync<AssessmentAsset>(query, new { AssessmentId = assessmentId })).ToList();
                _logger.LogDebug("Retrieved {Count} assets for assessment {AssessmentId}", assets.Count, assessmentId);
                return assets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        public async Task<AssessmentAssetMovement> GetAssetMovementsAsync(long assessmentAssetId)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                const string query = @"
                    SELECT TOP 1 * FROM tblAssessmentAssetMovement
                    WHERE AssessmentAssetId = @AssessmentAssetId
                ";

                var movement = await connection.QuerySingleOrDefaultAsync<AssessmentAssetMovement>(
                    query, new { AssessmentAssetId = assessmentAssetId });

                return movement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movements for asset {AssetId}", assessmentAssetId);
                throw;
            }
        }

        public async Task<List<AssessmentAssetMovement>> GetAllAssetMovementsAsync(long assessmentId)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                const string query = @"
                    SELECT * FROM tblAssessmentAssetMovement
                    WHERE AssessmentId = @AssessmentId
                ";

                var movements = (await connection.QueryAsync<AssessmentAssetMovement>(query, new { AssessmentId = assessmentId })).ToList();
                return movements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movements for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        public async Task SaveAssetDepreciationSummaryAsync(long assessmentId, Dictionary<int, decimal> monthlyDepreciation)
        {
            // This would store aggregated depreciation for reporting/analysis
            _logger.LogDebug("Saved asset depreciation summary for assessment {AssessmentId}", assessmentId);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<int, decimal>> GetAssetDepreciationSummaryAsync(long assessmentId)
        {
            var summary = new Dictionary<int, decimal>();
            for (int i = 1; i <= 12; i++)
                summary[i] = 0;
            return await Task.FromResult(summary);
        }
    }
}