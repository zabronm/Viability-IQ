using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.FinancialCalculations
{
   
    /// Handles depreciation calculations for assessment assets
    /// Supports multiple depreciation methods and mid-period acquisitions
   
    public class AssetDepreciationEngine : IAssetDepreciationEngine
    {
        private readonly IGenericDataRepository<AssessmentAsset> _assetRepository;
        private readonly IGenericDataRepository<AssessmentAssetMovement> _movementRepository;
        private readonly ILogger<AssetDepreciationEngine> _logger;

        public AssetDepreciationEngine(
            IGenericDataRepository<AssessmentAsset> assetRepository,
            IGenericDataRepository<AssessmentAssetMovement> movementRepository,
            ILogger<AssetDepreciationEngine> logger)
        {
            _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

       
        /// Calculates total depreciation expense for a specific month across all assessment assets
       
        public async Task<decimal> CalculateMonthDepreciationAsync(long assessmentId, int month)
        {
            try
            {
                _logger.LogDebug("Calculating depreciation for assessment {AssessmentId}, month {Month}", assessmentId, month);

                // Get all active assets for the assessment
                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                if (assets == null || assets.Count == 0)
                {
                    _logger.LogDebug("No active assets found for assessment {AssessmentId}", assessmentId);
                    return 0m;
                }

                decimal totalDepreciation = 0m;

                foreach (var asset in assets)
                {
                    // Check if asset is active in this month
                    if (!IsAssetActiveInMonth(asset, month))
                    {
                        _logger.LogDebug("Asset {AssetId} not active in month {Month}", asset.AssessmentAssetId, month);
                        continue;
                    }

                    // Calculate depreciation for this asset
                    decimal assetDepreciation = CalculateAssetMonthlyDepreciation(asset, month);
                    totalDepreciation += assetDepreciation;

                    _logger.LogDebug(
                        "Asset {AssetId} ({AssetName}): Depreciation={Depreciation} for month {Month}",
                        asset.AssessmentAssetId, asset.AssetName, assetDepreciation, month);
                }

                _logger.LogInformation(
                    "Total depreciation for assessment {AssessmentId}, month {Month}: {TotalDepreciation}",
                    assessmentId, month, totalDepreciation);

                return totalDepreciation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating depreciation for assessment {AssessmentId}, month {Month}", assessmentId, month);
                throw;
            }
        }

       
        /// Calculates depreciation for a specific asset in a specific month
       
        public decimal CalculateAssetMonthlyDepreciation(AssessmentAsset asset, int month)
        {
            if (asset == null || !asset.IsDepreciable)
                return 0m;

            if (asset.OpeningBalanceValue <= 0)
                return 0m;

            if (asset.DepreciationRate == null || asset.DepreciationRate <= 0)
                return 0m;

            // Calculate based on depreciation method
            return asset.DepreciationMethod switch
            {
                "Straight-Line" => CalculateStraightLineDepreciation(asset),
                "Declining-Balance" => CalculateDeciningBalanceDepreciation(asset, month),
                "Units-of-Production" => CalculateUnitsOfProductionDepreciation(asset, month),
                _ => CalculateStraightLineDepreciation(asset) // Default to straight-line
            };
        }

       
        /// Straight-line depreciation: Same amount each month
        /// Formula: (Acquisition Cost - Salvage Value) / Useful Life in Months       
        private decimal CalculateStraightLineDepreciation(AssessmentAsset asset)
        {
            if (asset.OpeningBalanceValue <= 0)
                return 0m;

            // Monthly depreciation = (Annual Rate / 100) * Cost / 12
            decimal monthlyDepreciation = (asset.OpeningBalanceValue * (asset.DepreciationRate)) / 100m / 12m;
            return Math.Round(monthlyDepreciation, 2);
        }

       
        /// Declining balance: Higher depreciation early, lower later
        /// Formula: (Book Value * Rate) / 12       
        private decimal CalculateDeciningBalanceDepreciation(AssessmentAsset asset, int month)
        {
            if (asset.OpeningBalanceValue <= 0)
                return 0m;

            // For simplicity, using fixed rate applied monthly
            // In real scenario, would track accumulated depreciation from movement records
            decimal monthlyRate = (asset.DepreciationRate) / 100m / 12m;
            decimal monthlyDepreciation = asset.OpeningBalanceValue * monthlyRate;

            return Math.Round(monthlyDepreciation, 2);
        }

       
        /// Units of production: Depreciation based on usage
        /// Requires additional usage data (not in current model)       
        private decimal CalculateUnitsOfProductionDepreciation(AssessmentAsset asset, int month)
        {
            // TODO: Implement when usage data model is available
            // For now, fall back to straight-line
            return CalculateStraightLineDepreciation(asset);
        }

       
        /// Checks if asset is active (owned) in a specific month
        /// Handles mid-period acquisitions       
        private bool IsAssetActiveInMonth(AssessmentAsset asset, int month)
        {
            if (asset.IsPreExisting)
            {
                // Pre-existing assets are active all 12 months
                return true;
            }
            else
            {
                // Mid-period acquired assets are active from acquisition month onwards
                return month >= asset.AcquisitionStartMonth;
            }
        }

       
        /// Gets monthly depreciation for all assets in an assessment
        /// Returns dictionary: month -> total depreciation       
        public async Task<Dictionary<int, decimal>> CalculateAnnualDepreciationAsync(long assessmentId)
        {
            var result = new Dictionary<int, decimal>();

            for (int month = 1; month <= 12; month++)
            {
                result[month] = await CalculateMonthDepreciationAsync(assessmentId, month);
            }

            return result;
        }

       
        /// Gets depreciation data from asset movement records
        /// Useful when depreciation has already been calculated and stored       
        public async Task<decimal> GetStoredMonthDepreciationAsync(long assessmentAssetId, int month)
        {
            try
            {
                var movements = (await _movementRepository.GetAllAsync(m =>
                    m.AssessmentAssetId == assessmentAssetId && m.Active)).FirstOrDefault();

                if (movements == null)
                    return 0m;

                return movements.GetDepreciation(month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stored depreciation for asset {AssetId}, month {Month}", assessmentAssetId, month);
                return 0m;
            }
        }
    }
}