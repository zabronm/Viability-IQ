using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.FinancialCalculations
{
    /// <summary>
    /// AssetEngine - Centralized engine for calculating asset depreciation and movement projections
    /// Aggregates asset movements and depreciation for integration into cashflow calculations
    /// </summary>
    public class AssetEngine : IAssetEngine
    {
        #region Private Fields

        private readonly IGenericDataRepository<AssessmentAsset> _assetRepository;
        private readonly IGenericDataRepository<AssessmentAssetMovement> _movementRepository;
        private readonly IProjectionStateManager _projectionStateManager;
        private readonly ILogger<AssetEngine> _logger;

        private const decimal DEPRECIATION_ROUNDING = 2; // Round to cents

        #endregion

        #region Constructor

        public AssetEngine(
            IGenericDataRepository<AssessmentAsset> assetRepository,
            IGenericDataRepository<AssessmentAssetMovement> movementRepository,
            IProjectionStateManager projectionStateManager,
            ILogger<AssetEngine> logger)
        {
            _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _projectionStateManager = projectionStateManager ?? throw new ArgumentNullException(nameof(projectionStateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculate 12-month asset depreciation for all assets in an assessment
        /// Returns aggregated depreciation by month
        /// </summary>
        public async Task<Dictionary<int, decimal>> CalculateTotalAssetDepreciationAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Calculating total asset depreciation for assessment {AssessmentId}", assessmentId);

                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                if (assets.Count == 0)
                {
                    _logger.LogDebug("No active assets found for assessment {AssessmentId}", assessmentId);
                    return InitializeMonthlyDictionary(0);
                }

                var totalDepreciation = InitializeMonthlyDictionary(0);

                // Calculate depreciation for each asset
                foreach (var asset in assets)
                {
                    var assetDepreciation = await CalculateAssetDepreciationAsync(asset);

                    // Aggregate by month
                    for (int month = 1; month <= 12; month++)
                    {
                        if (assetDepreciation.TryGetValue(month, out var monthDepr))
                        {
                            totalDepreciation[month] += monthDepr;
                        }
                    }
                }

                return totalDepreciation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total asset depreciation for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        /// <summary>
        /// Calculate 12-month depreciation for a single asset
        /// Accounts for asset acquisition month and depreciation method
        /// </summary>
        public async Task<Dictionary<int, decimal>> CalculateAssetDepreciationAsync(AssessmentAsset asset)
        {
            try
            {
                _logger.LogDebug("Calculating depreciation for asset {AssetId} (Name: {Name})",
                    asset.AssessmentAssetId, asset.AssetName);

                var depreciation = InitializeMonthlyDictionary(0);

                if (asset.OpeningBalanceValue <= 0)
                {
                    _logger.LogWarning("Asset {AssetId} has zero or negative value, no depreciation", asset.AssessmentAssetId);
                    return depreciation;
                }

                decimal monthlyDepreciationAmount = CalculateMonthlyDepreciationAmount(asset);
                int startMonth = asset.AcquisitionStartMonth > 0 ? asset.AcquisitionStartMonth : 1;

                // Calculate depreciation starting from acquisition month
                for (int month = startMonth; month <= 12; month++)
                {
                    depreciation[month] = Math.Round(monthlyDepreciationAmount, (int)DEPRECIATION_ROUNDING);
                }

                _logger.LogDebug(
                    "Asset {AssetId} depreciation: Monthly={Monthly}, StartMonth={StartMonth}",
                    asset.AssessmentAssetId, monthlyDepreciationAmount, startMonth);

                return depreciation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating depreciation for asset {AssetId}", asset.AssessmentAssetId);
                throw;
            }
        }

        /// <summary>
        /// Get gross value (opening + movements) for all assets by month
        /// </summary>
        public async Task<Dictionary<int, decimal>> CalculateTotalAssetGrossValueAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Calculating total asset gross value for assessment {AssessmentId}", assessmentId);

                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                var totalGrossValue = InitializeMonthlyDictionary(0);

                foreach (var asset in assets)
                {
                    var assetGrossValue = await CalculateAssetGrossValueAsync(asset);

                    for (int month = 1; month <= 12; month++)
                    {
                        if (assetGrossValue.TryGetValue(month, out var grossVal))
                        {
                            totalGrossValue[month] += grossVal;
                        }
                    }
                }

                return totalGrossValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total asset gross value for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        /// <summary>
        /// Calculate gross value (opening + movements) for a single asset
        /// </summary>
        public async Task<Dictionary<int, decimal>> CalculateAssetGrossValueAsync(AssessmentAsset asset)
        {
            try
            {
                var grossValue = InitializeMonthlyDictionary(asset.OpeningBalanceValue);
                int startMonth = asset.AcquisitionStartMonth > 0 ? asset.AcquisitionStartMonth : 1;

                // For pre-existing assets, start from month 1
                // For acquired assets, set months 1 to (startMonth-1) to 0
                if (asset.AcquisitionStartMonth > 1)
                {
                    for (int month = 1; month < asset.AcquisitionStartMonth; month++)
                    {
                        grossValue[month] = 0;
                    }
                }

                // Load movements and apply to gross value
                var movements = await GetAssetMovementsAsync(asset.AssessmentAssetId);

                if (movements != null)
                {
                    decimal runningGrossValue = asset.OpeningBalanceValue;

                    for (int month = startMonth; month <= 12; month++)
                    {
                        var movementType = movements.GetMovementType(month);
                        var movementValue = movements.GetMovementValue(month);

                        // Apply movement
                        runningGrossValue = ApplyMovement(runningGrossValue, movementType, movementValue);
                        grossValue[month] = Math.Max(runningGrossValue, 0); // Gross value cannot be negative
                    }
                }

                return grossValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating gross value for asset {AssetId}", asset.AssessmentAssetId);
                throw;
            }
        }

        /// <summary>
        /// Get all asset data with movements for display
        /// </summary>
        public async Task<List<AssetProjectionDto>> GetAllAssetsWithProjectionsAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Getting all assets with projections for assessment {AssessmentId}", assessmentId);

                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                var projections = new List<AssetProjectionDto>();

                foreach (var asset in assets)
                {
                    var grossValue = await CalculateAssetGrossValueAsync(asset);
                    var depreciation = await CalculateAssetDepreciationAsync(asset);
                    var movements = await GetAssetMovementsAsync(asset.AssessmentAssetId);

                    var projection = new AssetProjectionDto
                    {
                        AssessmentAssetId = asset.AssessmentAssetId,
                        AssetName = asset.AssetName,
                        AssetTypeId = asset.AssetTypeId,
                        OpeningBalanceValue = asset.OpeningBalanceValue,
                        DepreciationRate = asset.DepreciationRate,
                        AcquisitionStartMonth = asset.AcquisitionStartMonth,
                        MonthlyGrossValue = grossValue,
                        MonthlyDepreciation = depreciation,
                        HasMovements = movements != null
                    };

                    projections.Add(projection);
                }

                return projections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assets with projections for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        /// <summary>
        /// Recalculate all asset projections when movements change
        /// </summary>
        public async Task<bool> RecalculateAssetProjectionsAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Recalculating asset projections for assessment {AssessmentId}", assessmentId);

                // Invalidate asset cache
                await _projectionStateManager.InvalidateDataAsync("assets", assessmentId, assessmentId);

                _logger.LogInformation("Asset projections recalculated for assessment {AssessmentId}", assessmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating asset projections for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Calculate monthly depreciation amount
        /// </summary>
        private decimal CalculateMonthlyDepreciationAmount(AssessmentAsset asset)
        {
            // Annual depreciation = Opening Value × (Depreciation Rate / 100)
            // Monthly depreciation = Annual Depreciation / 12
            decimal annualDepreciation = asset.OpeningBalanceValue * (asset.DepreciationRate / 100m);
            decimal monthlyDepreciation = annualDepreciation / 12m;

            return Math.Round(monthlyDepreciation, (int)DEPRECIATION_ROUNDING);
        }

        /// <summary>
        /// Get movements for an asset
        /// </summary>
        private async Task<AssessmentAssetMovement> GetAssetMovementsAsync(long assessmentAssetId)
        {
            try
            {
                var movements = (await _movementRepository.GetAllAsync(m =>
                    m.AssessmentAssetId == assessmentAssetId)).FirstOrDefault();

                return movements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movements for asset {AssetId}", assessmentAssetId);
                return null;
            }
        }

        /// <summary>
        /// Apply movement to gross value based on movement type
        /// </summary>
        private decimal ApplyMovement(decimal currentGrossValue, string movementType, decimal movementValue)
        {
            if (string.IsNullOrEmpty(movementType))
                return currentGrossValue;

            return movementType switch
            {
                "Addition" => currentGrossValue + movementValue,
                "Disposal" => currentGrossValue - movementValue,
                "Revaluation" => currentGrossValue + movementValue, // Revaluation adds/subtracts
                "Transfer" => currentGrossValue, // Transfer doesn't affect gross value
                _ => currentGrossValue
            };
        }

        /// <summary>
        /// Initialize monthly dictionary with starting value
        /// </summary>
        private Dictionary<int, decimal> InitializeMonthlyDictionary(decimal initialValue)
        {
            var dict = new Dictionary<int, decimal>();
            for (int month = 1; month <= 12; month++)
            {
                dict[month] = initialValue;
            }
            return dict;
        }

        #endregion
    }

    /// <summary>
    /// AssetProjectionDto - DTO for displaying asset projections
    /// </summary>
    public class AssetProjectionDto
    {
        public long AssessmentAssetId { get; set; }
        public string AssetName { get; set; }
        public long AssetTypeId { get; set; }
        public decimal OpeningBalanceValue { get; set; }
        public decimal DepreciationRate { get; set; }
        public int AcquisitionStartMonth { get; set; }
        public Dictionary<int, decimal> MonthlyGrossValue { get; set; }
        public Dictionary<int, decimal> MonthlyDepreciation { get; set; }
        public bool HasMovements { get; set; }
    }
}
