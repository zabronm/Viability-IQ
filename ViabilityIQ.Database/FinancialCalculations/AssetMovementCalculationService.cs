using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations
{
    /// <summary>
    /// AssetMovementCalculationService - Comprehensive reusable service for calculating asset movements
    /// 
    /// Responsibilities:
    /// - Initialize movement templates for new assets
    /// - Recalculate all 12 months based on current data
    /// - Support cascading calculations from any month
    /// - Handle different depreciation methods (Straight-Line, Declining-Balance, Units-of-Production)
    /// - Generate monthly and annual summaries
    /// - Properly handle nullable decimals
    /// 
    /// Used by:
    /// - AssessmentAssetFormComponent (on asset creation)
    /// - AssetMovementFormComponent (on movement editing)
    /// 
    /// Features:
    /// ✅ Null-safe decimal operations
    /// ✅ Cascading calculations
    /// ✅ Mid-period asset acquisition support
    /// ✅ Multiple depreciation methods
    /// ✅ Comprehensive logging
    /// ✅ Validation and error handling
    /// </summary>
    public class AssetMovementCalculationService : IAssetMovementCalculationService
    {
        #region ===== PRIVATE FIELDS =====

        private readonly IGenericDataRepository<AssessmentAsset> _assetRepository;
        private readonly ILogger<AssetMovementCalculationService> _logger;

        #endregion

        #region ===== CONSTRUCTOR =====

        public AssetMovementCalculationService(
            IGenericDataRepository<AssessmentAsset> assetRepository,
            ILogger<AssetMovementCalculationService> logger)
        {
            _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region ===== PUBLIC METHODS =====

        /// <summary>
        /// Initializes default movement values for a new movement record
        /// Called when creating a new asset's movement template
        /// 
        /// Process:
        /// 1. Creates new AssessmentAssetMovement record
        /// 2. Initializes all 12 months with default values
        /// 3. For months before acquisition: all values = 0
        /// 4. For acquisition month: opening values from asset master
        /// 5. For subsequent months: carry forward with depreciation
        /// </summary>
        public async Task<AssessmentAssetMovement> InitializeMovementTemplateAsync(AssessmentAsset asset)
        {
            try
            {
                _logger.LogInformation(
                    "Initializing movement template for asset {AssetId} (Name: {AssetName}, IsPreExisting: {IsPreExisting})",
                    asset.AssessmentAssetId,
                    asset.AssetName,
                    asset.IsPreExisting);

                var movement = new AssessmentAssetMovement
                {
                    AssessmentAssetId = asset.AssessmentAssetId,
                    AssessmentId = asset.AssessmentId,
                    DepreciationRate = asset.DepreciationRate,              //ENSURE DEPRECIATION RATE IS NOT NULL HERE 
                    DepreciationMethod = asset.DepreciationMethod,          //ENSURE DEPRECIATION METHOD IS NOT NULL HERE
                    CreatedDate = DateTime.UtcNow,
                    Active = true
                };

                // Initialize all 12 months
                await InitializeDefaultMovementValuesAsync(movement, asset);

                _logger.LogInformation(
                    "Movement template initialized successfully for asset {AssetId}. " +
                    "Opening Value: {OpeningValue}, Depreciation Rate: {DepRate}%",
                    asset.AssessmentAssetId,
                    asset.OpeningBalanceValue,
                    asset.DepreciationRate);

                return movement;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error initializing movement template for asset {AssetId}",
                    asset.AssessmentAssetId);
                throw;
            }
        }

        /// <summary>
        /// Recalculates all 12 months based on current movement data and asset master data
        /// Called when user enters/edits monthly movements
        /// 
        /// Calculation Flow:
        /// 1. Iterate through all 12 months
        /// 2. For each month:
        ///    a. Check if asset is active
        ///    b. Get movement data (type and value)
        ///    c. Calculate ending gross value
        ///    d. Calculate monthly depreciation
        ///    e. Calculate accumulated depreciation
        ///    f. Calculate net book value
        ///    g. Carry forward to next month
        /// 
        /// ✅ Properly handles nullable decimals
        /// ✅ Validates accumulated depreciation doesn't exceed gross value
        /// ✅ Handles mid-period acquisitions
        /// </summary>
        public async Task<AssessmentAssetMovement> RecalculateAllMonthsAsync(
            AssessmentAssetMovement movement,
            AssessmentAsset asset)
        {
            try
            {
                _logger.LogInformation(
                    "Starting recalculation of all months for asset {AssetId}",
                    asset.AssessmentAssetId);

                decimal previousGrossValue = asset.OpeningBalanceValue;
                decimal previousAccumulatedDepr = asset.IsPreExisting ? asset.OpeningAccumulatedDepreciation : 0;

                _logger.LogDebug(
                    "Recalculation starting with Opening Gross: {Gross}, Opening Accum Depr: {AccumDepr}",
                    previousGrossValue,
                    previousAccumulatedDepr);

                // Process each month
                for (int month = 1; month <= 12; month++)
                {
                    // Check if asset is active in this month
                    if (!IsAssetActiveInMonth(asset, month))
                    {
                        ClearMonthData(movement, month);
                        _logger.LogDebug(
                            "Month {Month}: Asset not yet acquired (Acquisition starts month {AcquisitionMonth}), " +
                            "all values cleared",
                            month,
                            asset.AcquisitionStartMonth);
                        continue;
                    }

                    // ✅ STEP 1: Get movement data for this month
                    var movementType = movement.GetMovementType(month);
                    decimal movementValue = movement.GetMovementValue(month);

                    // ✅ STEP 2: Calculate ending gross value
                    decimal endingGrossValue = CalculateGrossValue(previousGrossValue, movementType, movementValue);
                    movement.SetGrossValue(month, endingGrossValue);

                    _logger.LogDebug(
                        "Month {Month}: Gross Value Calculation: {Previous} {Operation} {Movement} = {Result}",
                        month,
                        previousGrossValue,
                        !string.IsNullOrEmpty(movementType) ? $"({movementType})" : "(no movement)",
                        movementValue,
                        endingGrossValue);

                    // ✅ STEP 3: Calculate monthly depreciation
                    decimal monthlyDepreciation = CalculateMonthlyDepreciation(asset, endingGrossValue);
                    movement.SetDepreciation(month, monthlyDepreciation);

                    // ✅ STEP 4: Calculate accumulated depreciation
                    decimal endingAccumulatedDepr = previousAccumulatedDepr + monthlyDepreciation;

                    // Validate: Accumulated depreciation cannot exceed gross value
                    if (endingAccumulatedDepr > endingGrossValue)
                    {
                        _logger.LogWarning(
                            "Month {Month}: Validation Alert - Accumulated depreciation ({AccumDepr}) exceeds " +
                            "gross value ({GrossValue}). Capping to gross value.",
                            month,
                            endingAccumulatedDepr,
                            endingGrossValue);
                        endingAccumulatedDepr = endingGrossValue;
                    }

                    movement.SetAccumulatedDepreciation(month, endingAccumulatedDepr);

                    // ✅ STEP 5: Calculate net book value
                    decimal netBookValue = endingGrossValue - endingAccumulatedDepr;
                    movement.SetNetBookValue(month, netBookValue);

                    _logger.LogDebug(
                        "Month {Month}: Depreciation={Depr:C}, AccumDepr={AccumDepr:C}, NetBookValue={NBV:C}",
                        month,
                        monthlyDepreciation,
                        endingAccumulatedDepr,
                        netBookValue);

                    // ✅ STEP 6: Carry forward for next month
                    previousGrossValue = endingGrossValue;
                    previousAccumulatedDepr = endingAccumulatedDepr;
                }

                _logger.LogInformation(
                    "Recalculation complete for asset {AssetId}. " +
                    "Final Gross: {FinalGross:C}, Final AccumDepr: {FinalAccumDepr:C}",
                    asset.AssessmentAssetId,
                    previousGrossValue,
                    previousAccumulatedDepr);

                return movement;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recalculating months for asset {AssetId}",
                    asset.AssessmentAssetId);
                throw;
            }
        }

        /// <summary>
        /// Recalculates from a specific month onwards (cascade recalculation)
        /// Used when user edits a single month and needs to cascade changes to subsequent months
        /// 
        /// Example:
        /// - User changes Month 3 movement value
        /// - This method recalculates Month 3-12 to reflect the change
        /// - Previous months (1-2) remain unchanged
        /// 
        /// ✅ Properly handles nullable decimals
        /// ✅ Validates month range
        /// ✅ Supports mid-period acquisitions
        /// </summary>
        public async Task<AssessmentAssetMovement> RecalculateFromMonthAsync(
            AssessmentAssetMovement movement,
            AssessmentAsset asset,
            int fromMonth)
        {
            try
            {
                _logger.LogInformation(
                    "Starting cascade recalculation from month {FromMonth} for asset {AssetId}",
                    fromMonth,
                    asset.AssessmentAssetId);

                // ✅ Validate month range
                if (fromMonth < 1 || fromMonth > 12)
                {
                    _logger.LogError(
                        "Invalid month {FromMonth} provided for recalculation. Must be between 1 and 12.",
                        fromMonth);
                    throw new ArgumentException(
                        $"Month must be between 1 and 12, received {fromMonth}",
                        nameof(fromMonth));
                }

                // Get the starting values from the previous month
                // ✅ Handle nullable decimals with helper methods
                decimal previousGrossValue = GetPreviousMonthGrossValue(movement, asset, fromMonth);
                decimal previousAccumulatedDepr = GetPreviousMonthAccumulatedDepreciation(movement, asset, fromMonth);

                _logger.LogDebug(
                    "Cascade starting from month {FromMonth}. Previous Gross: {Gross:C}, " +
                    "Previous AccumDepr: {AccumDepr:C}",
                    fromMonth,
                    previousGrossValue,
                    previousAccumulatedDepr);

                // Recalculate from the specified month onwards
                for (int month = fromMonth; month <= 12; month++)
                {
                    if (!IsAssetActiveInMonth(asset, month))
                    {
                        ClearMonthData(movement, month);
                        _logger.LogDebug("Month {Month}: Asset not active, data cleared", month);
                        continue;
                    }

                    var movementType = movement.GetMovementType(month);
                    decimal movementValue = movement.GetMovementValue(month);

                    decimal endingGrossValue = CalculateGrossValue(previousGrossValue, movementType, movementValue);
                    movement.SetGrossValue(month, endingGrossValue);

                    decimal monthlyDepreciation = CalculateMonthlyDepreciation(asset, endingGrossValue);
                    movement.SetDepreciation(month, monthlyDepreciation);

                    decimal endingAccumulatedDepr = previousAccumulatedDepr + monthlyDepreciation;
                    if (endingAccumulatedDepr > endingGrossValue)
                    {
                        _logger.LogWarning(
                            "Month {Month}: Capping accumulated depreciation from {AccumDepr:C} to {Gross:C}",
                            month,
                            endingAccumulatedDepr,
                            endingGrossValue);
                        endingAccumulatedDepr = endingGrossValue;
                    }

                    movement.SetAccumulatedDepreciation(month, endingAccumulatedDepr);

                    decimal netBookValue = endingGrossValue - endingAccumulatedDepr;
                    movement.SetNetBookValue(month, netBookValue);

                    _logger.LogDebug(
                        "Month {Month}: Gross={Gross:C}, Depr={Depr:C}, AccumDepr={AccumDepr:C}, NBV={NBV:C}",
                        month,
                        endingGrossValue,
                        monthlyDepreciation,
                        endingAccumulatedDepr,
                        netBookValue);

                    previousGrossValue = endingGrossValue;
                    previousAccumulatedDepr = endingAccumulatedDepr;
                }

                _logger.LogInformation(
                    "Cascade recalculation complete from month {FromMonth} for asset {AssetId}",
                    fromMonth,
                    asset.AssessmentAssetId);

                return movement;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recalculating from month {FromMonth} for asset {AssetId}",
                    fromMonth,
                    asset.AssessmentAssetId);
                throw;
            }
        }

        /// <summary>
        /// Gets a summary of all 12 months for display in UI tables/grids
        /// 
        /// ✅ Properly handles nullable decimals with null coalescing operator
        /// ✅ Returns display-ready data
        /// ✅ Includes formatted strings for UI display
        /// </summary>
        public List<AssetMovementMonthlySummaryDto> GetMonthlySummary(
            AssessmentAssetMovement movement,
            AssessmentAsset asset)
        {
            try
            {
                _logger.LogDebug(
                    "Generating monthly summary for asset {AssetId}",
                    asset.AssessmentAssetId);

                var summary = new List<AssetMovementMonthlySummaryDto>();

                for (int month = 1; month <= 12; month++)
                {
                    // ✅ Handle nullable decimals with null coalescing operator (??)
                    summary.Add(new AssetMovementMonthlySummaryDto
                    {
                        Month = month,
                        MonthName = GetMonthName(month),
                        MovementType = movement.GetMovementType(month) ?? "None",
                        MovementValue = movement.GetMovementValue(month),
                        GrossValue = movement.GetGrossValue(month) ?? 0,
                        Depreciation = movement.GetDepreciation(month),
                        AccumulatedDepreciation = movement.GetAccumulatedDepreciation(month) ?? 0,
                        NetBookValue = movement.GetNetBookValue(month) ?? 0,
                        IsAssetActive = IsAssetActiveInMonth(asset, month)
                    });
                }

                _logger.LogDebug(
                    "Monthly summary generated: {MonthCount} months, " +
                    "Final NBV: {FinalNBV:C}",
                    summary.Count,
                    summary.Last().NetBookValue);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting monthly summary for asset {AssetId}",
                    asset.AssessmentAssetId);
                throw;
            }
        }

        /// <summary>
        /// Gets annual totals and summary for reporting
        /// 
        /// ✅ Properly handles nullable decimals
        /// ✅ Calculates comprehensive annual metrics
        /// ✅ Returns aggregated data for reports
        /// </summary>
        public AssetMovementAnnualSummaryDto GetAnnualSummary(
            AssessmentAssetMovement movement,
            AssessmentAsset asset)
        {
            try
            {
                _logger.LogDebug(
                    "Generating annual summary for asset {AssetId}",
                    asset.AssessmentAssetId);

                var monthlySummary = GetMonthlySummary(movement, asset);
                var lastMonth = monthlySummary.Last();

                var annualSummary = new AssetMovementAnnualSummaryDto
                {
                    AssetId = asset.AssessmentAssetId,
                    AssetName = asset.AssetName,
                    OpeningGrossValue = asset.OpeningBalanceValue,
                    OpeningAccumulatedDepreciation = asset.OpeningAccumulatedDepreciation,
                    OpeningNetBookValue = asset.OpeningBalanceValue - asset.OpeningAccumulatedDepreciation,
                    // ✅ Handle nullable decimals with null coalescing
                    ClosingGrossValue = lastMonth.GrossValue,
                    ClosingAccumulatedDepreciation = lastMonth.AccumulatedDepreciation,
                    ClosingNetBookValue = lastMonth.NetBookValue,
                    TotalDepreciation = monthlySummary.Sum(m => m.Depreciation),
                    TotalAdditions = monthlySummary
                        .Where(m => m.MovementType == "Addition")
                        .Sum(m => m.MovementValue),
                    TotalDisposals = monthlySummary
                        .Where(m => m.MovementType == "Disposal")
                        .Sum(m => m.MovementValue),
                    TotalRevaluations = monthlySummary
                        .Where(m => m.MovementType == "Revaluation")
                        .Sum(m => m.MovementValue)
                };

                _logger.LogInformation(
                    "Annual summary generated for asset {AssetId}: " +
                    "Opening NBV={OpeningNBV:C}, Closing NBV={ClosingNBV:C}, " +
                    "Total Depreciation={TotalDepr:C}",
                    asset.AssessmentAssetId,
                    annualSummary.OpeningNetBookValue,
                    annualSummary.ClosingNetBookValue,
                    annualSummary.TotalDepreciation);

                return annualSummary;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting annual summary for asset {AssetId}",
                    asset.AssessmentAssetId);
                throw;
            }
        }

        #endregion

        #region ===== PRIVATE HELPER METHODS =====

        /// <summary>
        /// Initializes default values for all 12 months
        /// Sets opening values for acquisition month and carries forward for subsequent months
        /// </summary>
        private async Task InitializeDefaultMovementValuesAsync(
            AssessmentAssetMovement movement,
            AssessmentAsset asset)
        {
            try
            {
                for (int month = 1; month <= 12; month++)
                {
                    if (!IsAssetActiveInMonth(asset, month))
                    {
                        // Asset not yet acquired - clear all values
                        ClearMonthData(movement, month);
                    }
                    else
                    {
                        // Asset is active in this month
                        int acquisitionMonth = asset.AcquisitionStartMonth > 0 ? asset.AcquisitionStartMonth : 1;

                        if (month == acquisitionMonth)
                        {
                            // Opening month - set opening values from asset master
                            movement.SetMovementType(month, null);
                            movement.SetMovementValue(month, 0);
                            movement.SetGrossValue(month, asset.OpeningBalanceValue);
                            movement.SetAccumulatedDepreciation(month, asset.OpeningAccumulatedDepreciation);
                        }
                        else if (month > 1)
                        {
                            // Subsequent months - carry forward previous month's values
                            // ✅ Handle nullable decimals with null coalescing
                            decimal previousGross = movement.GetGrossValue(month - 1) ?? 0;
                            decimal previousAccum = movement.GetAccumulatedDepreciation(month - 1) ?? 0;

                            movement.SetMovementType(month, null);
                            movement.SetMovementValue(month, 0);
                            movement.SetGrossValue(month, previousGross);
                            movement.SetAccumulatedDepreciation(month, previousAccum);
                        }

                        // Calculate depreciation for this month
                        decimal currentGrossValue = movement.GetGrossValue(month) ?? 0;
                        decimal monthlyDepr = CalculateMonthlyDepreciation(asset, currentGrossValue);
                        movement.SetDepreciation(month, monthlyDepr);

                        // Calculate NBV
                        decimal currentAccum = (movement.GetAccumulatedDepreciation(month) ?? 0) + monthlyDepr;
                        decimal nbv = currentGrossValue - currentAccum;
                        movement.SetNetBookValue(month, nbv);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default movement values");
                throw;
            }
        }

        /// <summary>
        /// Checks if asset is active (owned) in a specific month
        /// Pre-existing assets: active all 12 months
        /// Acquired assets: active from acquisition month onwards
        /// </summary>
        private bool IsAssetActiveInMonth(AssessmentAsset asset, int month)
        {
            if (asset.IsPreExisting)
                return true;

            return month >= asset.AcquisitionStartMonth;
        }

        /// <summary>
        /// Calculates ending gross value based on movement type
        /// Supports: Addition, Disposal, Revaluation, Transfer
        /// </summary>
        private decimal CalculateGrossValue(decimal previousGross, string? movementType, decimal movementValue)
        {
            if (string.IsNullOrEmpty(movementType) || movementValue == 0)
                return previousGross;

            decimal newGross = movementType switch
            {
                "Addition" => previousGross + movementValue,
                "Disposal" => previousGross - movementValue,
                "Revaluation" => previousGross + movementValue,
                "Transfer" => previousGross, // Transfer doesn't affect asset value
                _ => previousGross
            };

            return Math.Max(newGross, 0); // Ensure non-negative
        }

        /// <summary>
        /// Calculates monthly depreciation based on asset depreciation method
        /// Supports: Straight-Line, Declining-Balance, Units-of-Production
        /// </summary>
        private decimal CalculateMonthlyDepreciation(AssessmentAsset asset, decimal grossValue)
        {
            if (!asset.IsDepreciable || asset.DepreciationRate == null || asset.DepreciationRate <= 0)
                return 0;

            if (string.IsNullOrEmpty(asset.DepreciationMethod))
                asset.DepreciationMethod = "Straight-Line"; // Default method

            return asset.DepreciationMethod switch
            {
                "Straight-Line" => CalculateStraightLineDepreciation(grossValue, asset.DepreciationRate),
                "Declining-Balance" => CalculateDecliningBalanceDepreciation(grossValue, asset.DepreciationRate),
                "Units-of-Production" => CalculateUnitsOfProductionDepreciation(grossValue, asset.DepreciationRate),
                _ => CalculateStraightLineDepreciation(grossValue, asset.DepreciationRate) // Fallback
            };
        }

        /// <summary>
        /// Straight-line depreciation: same amount each month
        /// Formula: (Gross Value × Annual Rate %) / 12
        /// Example: $1000 × 10% / 12 = $8.33 per month
        /// </summary>
        private decimal CalculateStraightLineDepreciation(decimal grossValue, decimal annualRate)
        {
            return (grossValue * (annualRate / 100m)) / 12m;
        }

        /// <summary>
        /// Declining-balance depreciation: higher early on, decreases over time
        /// Formula: (Gross Value × Annual Rate % × 2) / 12
        /// Double declining balance method
        /// </summary>
        private decimal CalculateDecliningBalanceDepreciation(decimal grossValue, decimal annualRate)
        {
            return (grossValue * (annualRate / 100m) * 2m) / 12m;
        }

        /// <summary>
        /// Units-of-production depreciation: based on usage
        /// For now, fallback to straight-line (requires usage/production data)
        /// Can be enhanced when production data model is available
        /// </summary>
        private decimal CalculateUnitsOfProductionDepreciation(decimal grossValue, decimal annualRate)
        {
            // TODO: Implement proper units-of-production calculation when usage data is available
            return CalculateStraightLineDepreciation(grossValue, annualRate);
        }

        /// <summary>
        /// Gets previous month's gross value with proper nullable handling
        /// ✅ Safely converts decimal? to decimal
        /// </summary>
        private decimal GetPreviousMonthGrossValue(
            AssessmentAssetMovement movement,
            AssessmentAsset asset,
            int fromMonth)
        {
            if (fromMonth <= 1)
                return asset.OpeningBalanceValue;

            return movement.GetGrossValue(fromMonth - 1) ?? 0;
        }

        /// <summary>
        /// Gets previous month's accumulated depreciation with proper nullable handling
        /// ✅ Safely converts decimal? to decimal
        /// </summary>
        private decimal GetPreviousMonthAccumulatedDepreciation(
            AssessmentAssetMovement movement,
            AssessmentAsset asset,
            int fromMonth)
        {
            if (fromMonth <= 1)
                return asset.IsPreExisting ? asset.OpeningAccumulatedDepreciation : 0;

            return movement.GetAccumulatedDepreciation(fromMonth - 1) ?? 0;
        }

        /// <summary>
        /// Clears all movement data for a specific month
        /// Sets all values to 0/null
        /// </summary>
        private void ClearMonthData(AssessmentAssetMovement movement, int month)
        {
            movement.SetMovementType(month, null);
            movement.SetMovementValue(month, 0);
            movement.SetDepreciation(month, 0);
            movement.SetGrossValue(month, 0);
            movement.SetAccumulatedDepreciation(month, 0);
            movement.SetNetBookValue(month, 0);
        }

        /// <summary>
        /// Gets month name from month number (1-12)
        /// </summary>
        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January",
                2 => "February",
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => $"Month {month}"
            };
        }

        #endregion
    }
}