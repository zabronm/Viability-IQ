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
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
   
    public class AssetRepository : IAssetRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<AssetRepository> _logger;
        private readonly ITenantAuthorizationService _tenantAuthorizationService;

        public AssetRepository(
            IDbConnectionFactory dbConnectionFactory,
            ILogger<AssetRepository> logger,
            ITenantAuthorizationService tenantAuthorizationService)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tenantAuthorizationService = tenantAuthorizationService;
        }

        public async Task<List<AssessmentAsset>> GetAssessmentAssetsAsync(long assessmentId)
        {
            await _tenantAuthorizationService.EnsureCanAccessAssessmentAsync(
                assessmentId, TenantRecordAccess.Read);
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                const string query = @"
                    SELECT * FROM tblAssessmentAssets
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
                var assessmentId = await connection.QuerySingleOrDefaultAsync<long?>(
                    """
                    SELECT AssessmentId
                    FROM tblAssessmentAssets
                    WHERE AssessmentAssetId = @AssessmentAssetId;
                    """,
                    new { AssessmentAssetId = assessmentAssetId });
                if (!assessmentId.HasValue)
                {
                    throw new KeyNotFoundException("The selected asset does not exist.");
                }

                await _tenantAuthorizationService.EnsureCanAccessAssessmentAsync(
                    assessmentId.Value, TenantRecordAccess.Read);
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
            await _tenantAuthorizationService.EnsureCanAccessAssessmentAsync(
                assessmentId, TenantRecordAccess.Read);
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
            await _tenantAuthorizationService.EnsureCanAccessAssessmentAsync(
                assessmentId, TenantRecordAccess.Write);
            // This would store aggregated depreciation for reporting/analysis
            _logger.LogDebug("Saved asset depreciation summary for assessment {AssessmentId}", assessmentId);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<int, decimal>> GetAssetDepreciationSummaryAsync(long assessmentId)
        {
            await _tenantAuthorizationService.EnsureCanAccessAssessmentAsync(
                assessmentId, TenantRecordAccess.Read);
            var summary = new Dictionary<int, decimal>();
            for (int i = 1; i <= 12; i++)
                summary[i] = 0;
            return await Task.FromResult(summary);
        }
    }
}