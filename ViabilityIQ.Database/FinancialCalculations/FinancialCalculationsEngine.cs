using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Modules;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.FinancialCalculations
{
    /// <summary>
    /// FinancialCalculationsEngine - Comprehensive engine for all financial calculations
    /// 
    /// Responsibilities:
    /// - Loan calculations (Reducing Balance, Flat Rate, Fixed Principal, Balloon, Interest Only)
    /// - Asset depreciation calculations (Straight-Line, Declining-Balance, Units-of-Production)
    /// - Assessment-level financial integration
    /// - Depreciation scheduling and reporting
    /// 
    /// NOTE: This engine does NOT handle cache invalidation. That's done at the component/API layer
    /// to prevent circular dependencies with IProjectionStateManager.
    /// 
    /// Dependencies:
    /// - IGenericDataRepository<Assessment>
    /// - IGenericDataRepository<AssessmentAsset>
    /// - IGenericDataRepository<AssessmentAssetMovement>
    /// - ILogger<FinancialCalculationsEngine>
    /// </summary>
    public class FinancialCalculationsEngine : IFinancialCalculationsEngine
    {
        #region ===== PRIVATE FIELDS =====

        private readonly IGenericDataRepository<Assessment> _assessmentRepository;
        private readonly IGenericDataRepository<AssessmentAsset> _assetRepository;
        private readonly IGenericDataRepository<AssessmentAssetMovement> _movementRepository;
        private readonly ILogger<FinancialCalculationsEngine> _logger;

        #endregion

        #region ===== CONSTRUCTOR =====

        public FinancialCalculationsEngine(
            IGenericDataRepository<Assessment> assessmentRepository,
            IGenericDataRepository<AssessmentAsset> assetRepository,
            IGenericDataRepository<AssessmentAssetMovement> movementRepository,
            ILogger<FinancialCalculationsEngine> logger)
        {
            _assessmentRepository = assessmentRepository ?? throw new ArgumentNullException(nameof(assessmentRepository));
            _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region ===== LOAN CALCULATION METHODS =====

        /// <summary>
        /// Calculates loan repayment schedule based on calculation method
        /// Supports: ReducingBalance, FlatRate, InterestOnly, FixedPrincipal, Balloon
        /// </summary>
        public LoanCalculationResults CalculateLoan(AssessmentLoan loan, LoanCalculationMethodsEnums method)
        {
            try
            {
                _logger.LogDebug(
                    "Calculating loan {LoanId} using method {Method}. Balance: {Balance:C}, Rate: {Rate}%, Months: {Months}",
                    loan.AssessmentLoanId,
                    method,
                    loan.LoanBalanceAtAssessmentDate,
                    loan.InterestRatePerAnnum,
                    loan.RepaymentPeriodMonths);

                var result = method switch
                {
                    LoanCalculationMethodsEnums.ReducingBalance => CalculateReducingBalance(loan),
                    LoanCalculationMethodsEnums.FlatRate => CalculateFlatRate(loan),
                    LoanCalculationMethodsEnums.InterestOnly => CalculateInterestOnly(loan),
                    LoanCalculationMethodsEnums.FixedPrincipal => CalculateFixedPrincipal(loan),
                    LoanCalculationMethodsEnums.Balloon => CalculateBalloon(loan),
                    _ => throw new NotSupportedException($"Loan calculation method {method} is not supported")
                };

                _logger.LogDebug(
                    "Loan calculation complete for {LoanId}. Monthly Payment: {Payment:C}",
                    loan.AssessmentLoanId,
                    result.MonthlyRepayment);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating loan {LoanId}", loan.AssessmentLoanId);
                throw;
            }
        }

        /// <summary>
        /// Builds repayment records for a loan
        /// Creates separate records for:
        /// - Expected repayment (metric type 1)
        /// - Interest (metric type 2)
        /// - Extra repayment (metric type 3)
        /// </summary>
        public List<AssessmentLoanRepayment> BuildRepaymentRecords(AssessmentLoan loan, LoanCalculationMethodsEnums method)
        {
            try
            {
                _logger.LogInformation(
                    "Building repayment records for loan {LoanId} using method {Method}",
                    loan.AssessmentLoanId,
                    method);

                var calculation = CalculateLoan(loan, method);
                var rows = new List<AssessmentLoanRepayment>();

                // Create metric type records
                rows.Add(CreateRepaymentRow(loan, 1, calculation.ExpectedRepayment));    // Expected repayment
                rows.Add(CreateRepaymentRow(loan, 2, calculation.Interest));             // Interest
                rows.Add(CreateRepaymentRow(loan, 3, calculation.ExtraRepayment));       // Extra repayment

                _logger.LogInformation(
                    "Repayment records built: {Count} records for loan {LoanId}",
                    rows.Count,
                    loan.AssessmentLoanId);

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building repayment records for loan {LoanId}", loan.AssessmentLoanId);
                throw;
            }
        }

        #endregion

        #region ===== ASSET DEPRECIATION METHODS =====

        /// <summary>
        /// Calculates total depreciation for all assets in an assessment for a specific month
        /// Aggregates depreciation across all active assets
        /// </summary>
        public async Task<decimal> CalculateMonthlyDepreciationAsync(long assessmentId, int month)
        {
            try
            {
                _logger.LogDebug(
                    "Calculating total depreciation for assessment {AssessmentId}, month {Month}",
                    assessmentId,
                    month);

                // Get all active assets for this assessment
                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                if (assets == null || assets.Count == 0)
                {
                    _logger.LogDebug("No active assets found for assessment {AssessmentId}", assessmentId);
                    return 0;
                }

                decimal totalDepreciation = 0;

                foreach (var asset in assets)
                {
                    // Check if asset is active in this month
                    if (!IsAssetActiveInMonth(asset, month))
                    {
                        _logger.LogDebug(
                            "Asset {AssetId} not active in month {Month} (acquired month {AcqMonth})",
                            asset.AssessmentAssetId,
                            month,
                            asset.AcquisitionStartMonth);
                        continue;
                    }

                    // Calculate depreciation for this asset
                    decimal assetDepreciation = CalculateAssetDepreciationForMonth(asset, month);
                    totalDepreciation += assetDepreciation;

                    _logger.LogDebug(
                        "Asset {AssetId} depreciation for month {Month}: {Depreciation:C}",
                        asset.AssessmentAssetId,
                        month,
                        assetDepreciation);
                }

                _logger.LogDebug(
                    "Total depreciation for assessment {AssessmentId}, month {Month}: {TotalDepreciation:C}",
                    assessmentId,
                    month,
                    totalDepreciation);

                return totalDepreciation;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error calculating monthly depreciation for assessment {AssessmentId}, month {Month}",
                    assessmentId,
                    month);
                throw;
            }
        }

        /// <summary>
        /// Calculates depreciation schedule for all assets for all 12 months
        /// Returns array of total depreciation by month [0-11]
        /// </summary>
        public async Task<decimal[]> CalculateAnnualDepreciationScheduleAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation(
                    "Calculating annual depreciation schedule for assessment {AssessmentId}",
                    assessmentId);

                var depreciationSchedule = new decimal[12];

                // Calculate for each month
                for (int month = 1; month <= 12; month++)
                {
                    depreciationSchedule[month - 1] = await CalculateMonthlyDepreciationAsync(assessmentId, month);
                }

                decimal totalAnnual = depreciationSchedule.Sum();
                decimal averageMonthly = depreciationSchedule.Average();

                _logger.LogInformation(
                    "Annual depreciation schedule calculated for assessment {AssessmentId}. " +
                    "Total: {Total:C}, Average: {Average:C}",
                    assessmentId,
                    totalAnnual,
                    averageMonthly);

                return depreciationSchedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error calculating annual depreciation schedule for assessment {AssessmentId}",
                    assessmentId);
                throw;
            }
        }

        /// <summary>
        /// Gets detailed depreciation breakdown by asset for a specific month
        /// Used for reporting and detailed views
        /// </summary>
        public async Task<List<AssetDepreciationMonthlyDto>> GetMonthlyDepreciationDetailAsync(long assessmentId, int month)
        {
            try
            {
                _logger.LogDebug(
                    "Getting depreciation detail for assessment {AssessmentId}, month {Month}",
                    assessmentId,
                    month);

                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                var details = new List<AssetDepreciationMonthlyDto>();

                foreach (var asset in assets)
                {
                    if (!IsAssetActiveInMonth(asset, month))
                        continue;

                    var movement = await GetAssetMovementAsync(asset.AssessmentAssetId);
                    var depreciation = CalculateAssetDepreciationForMonth(asset, month);
                    var grossValue = GetAssetGrossValueForMonth(asset, movement, month);
                    var accumulatedDepreciation = GetAssetAccumulatedDepreciationForMonth(asset, movement, month);
                    var netBookValue = grossValue - accumulatedDepreciation;

                    details.Add(new AssetDepreciationMonthlyDto
                    {
                        AssessmentAssetId = asset.AssessmentAssetId,
                        AssetName = asset.AssetName,
                        Month = month,
                        DepreciationMethod = asset.DepreciationMethod,
                        DepreciationRate = asset.DepreciationRate,
                        MonthlyDepreciation = depreciation,
                        GrossValue = grossValue,
                        AccumulatedDepreciation = accumulatedDepreciation,
                        NetBookValue = netBookValue,
                        IsActive = true
                    });
                }

                _logger.LogDebug(
                    "Depreciation detail retrieved: {AssetCount} assets for month {Month}",
                    details.Count,
                    month);

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting monthly depreciation detail for assessment {AssessmentId}, month {Month}",
                    assessmentId,
                    month);
                throw;
            }
        }

        /// <summary>
        /// Gets depreciation summary for entire assessment
        /// Includes totals, averages, and reporting data
        /// </summary>
        public async Task<AssetDepreciationSummaryDto> GetDepreciationSummaryAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation(
                    "Calculating depreciation summary for assessment {AssessmentId}",
                    assessmentId);

                var assets = (await _assetRepository.GetAllAsync(a =>
                    a.AssessmentId == assessmentId && a.Active)).ToList();

                var schedule = await CalculateAnnualDepreciationScheduleAsync(assessmentId);

                var summary = new AssetDepreciationSummaryDto
                {
                    AssessmentId = assessmentId,
                    TotalAssetsCount = assets.Count,
                    TotalDepreciableAssetsCount = assets.Count(a => a.IsDepreciable),
                    TotalAnnualDepreciation = schedule.Sum(),
                    AverageMonthlyDepreciation = schedule.Average(),
                    HighestMonthlyDepreciation = schedule.Max(),
                    LowestMonthlyDepreciation = schedule.Min(),
                    MonthlyDepreciationSchedule = schedule.ToList(),
                    CalculatedAt = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Depreciation summary calculated for assessment {AssessmentId}. " +
                    "Total Assets: {TotalAssets}, Depreciable: {DepreciableAssets}, " +
                    "Total Annual Depreciation: {Total:C}",
                    assessmentId,
                    summary.TotalAssetsCount,
                    summary.TotalDepreciableAssetsCount,
                    summary.TotalAnnualDepreciation);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error calculating depreciation summary for assessment {AssessmentId}",
                    assessmentId);
                throw;
            }
        }

        #endregion

        #region ===== ASSESSMENT FINANCIAL INTEGRATION =====

        /// <summary>
        /// Recalculates all financial data for an assessment
        /// This method performs calculations only. Cache invalidation is handled
        /// at the component/API layer to prevent circular dependencies.
        /// </summary>
        public async Task<bool> RecalculateAssessmentTotalsAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation(
                    "Starting assessment total recalculation for assessment {AssessmentId}",
                    assessmentId);

                // Recalculate asset-related values
                var success = await RecalculateAssetTotalsAsync(assessmentId);

                _logger.LogInformation(
                    "Assessment total recalculation completed for assessment {AssessmentId}",
                    assessmentId);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recalculating assessment totals for assessment {AssessmentId}",
                    assessmentId);
                throw;
            }
        }

        /// <summary>
        /// Recalculates asset-related totals for an assessment
        /// </summary>
        public async Task<bool> RecalculateAssetTotalsAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation(
                    "Recalculating asset totals for assessment {AssessmentId}",
                    assessmentId);

                // Get depreciation summary (performs all calculations)
                var depreciationSummary = await GetDepreciationSummaryAsync(assessmentId);

                _logger.LogInformation(
                    "Asset recalculation complete for assessment {AssessmentId}: " +
                    "Annual Depreciation={Depreciation:C}, Assets={Count}",
                    assessmentId,
                    depreciationSummary.TotalAnnualDepreciation,
                    depreciationSummary.TotalAssetsCount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recalculating asset totals for assessment {AssessmentId}",
                    assessmentId);
                throw;
            }
        }

        #endregion

        #region ===== PRIVATE LOAN CALCULATION METHODS =====

        /// <summary>
        /// Calculates loan using Reducing Balance method
        /// Most common method for installment loans
        /// </summary>
        private LoanCalculationResults CalculateReducingBalance(AssessmentLoan loan)
        {
            try
            {
                LoanCalculationResults result = new();

                decimal balance = loan.LoanBalanceAtAssessmentDate;
                decimal payment = FinancialMath.CalculateMonthlyRepayment(
                    balance,
                    loan.InterestRatePerAnnum,
                    loan.RepaymentPeriodMonths);

                result.MonthlyRepayment = payment;
                decimal monthlyRate = loan.InterestRatePerAnnum / 12m / 100m;

                int startIndex = Math.Max(0, loan.StartMonth - 1);
                int repaymentMonths = Math.Min(loan.RepaymentPeriodMonths, 12 - startIndex);

                for (int i = 0; i < repaymentMonths; i++)
                {
                    decimal interest = Math.Round(balance * monthlyRate, 2);
                    decimal principal = payment - interest;

                    if (principal > balance)
                        principal = balance;

                    balance -= principal;

                    result.ExpectedRepayment[startIndex + i] = payment;
                    result.Interest[startIndex + i] = interest;
                    result.Principal[startIndex + i] = principal;
                    result.OutstandingBalance[startIndex + i] = Math.Max(balance, 0);
                    result.ExtraRepayment[startIndex + i] = 0;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating reducing balance loan");
                throw;
            }
        }

        /// <summary>
        /// Calculates loan using Flat Rate method
        /// (Not yet implemented)
        /// </summary>
        private LoanCalculationResults CalculateFlatRate(AssessmentLoan loan)
        {
            _logger.LogWarning("Flat rate loan calculation requested for loan {LoanId} - not yet implemented", loan.AssessmentLoanId);
            throw new NotImplementedException("Flat rate calculation not yet implemented");
        }

        /// <summary>
        /// Calculates loan using Interest Only method
        /// (Not yet implemented)
        /// </summary>
        private LoanCalculationResults CalculateInterestOnly(AssessmentLoan loan)
        {
            _logger.LogWarning("Interest only loan calculation requested for loan {LoanId} - not yet implemented", loan.AssessmentLoanId);
            throw new NotImplementedException("Interest only calculation not yet implemented");
        }

        /// <summary>
        /// Calculates loan using Fixed Principal method
        /// (Not yet implemented)
        /// </summary>
        private LoanCalculationResults CalculateFixedPrincipal(AssessmentLoan loan)
        {
            _logger.LogWarning("Fixed principal loan calculation requested for loan {LoanId} - not yet implemented", loan.AssessmentLoanId);
            throw new NotImplementedException("Fixed principal calculation not yet implemented");
        }

        /// <summary>
        /// Calculates loan using Balloon method
        /// (Not yet implemented)
        /// </summary>
        private LoanCalculationResults CalculateBalloon(AssessmentLoan loan)
        {
            _logger.LogWarning("Balloon loan calculation requested for loan {LoanId} - not yet implemented", loan.AssessmentLoanId);
            throw new NotImplementedException("Balloon calculation not yet implemented");
        }

        /// <summary>
        /// Creates a repayment record row
        /// CRITICAL: Active flag MUST be true, otherwise record won't be used in calculations
        /// </summary>
        private AssessmentLoanRepayment CreateRepaymentRow(AssessmentLoan loan, int metricTypeId, decimal[] values)
        {
            var row = new AssessmentLoanRepayment
            {
                AssessmentId = loan.AssessmentId,
                AssessmentLoanId = loan.AssessmentLoanId,
                MetricTypeId = metricTypeId,
                AssessmentLoanRepaymentId = 0,
                MonthlyValues = values,
                Active = true  // ⚠️ EXTREMELY IMPORTANT - otherwise record won't be used
            };

            return row;
        }

        #endregion

        #region ===== PRIVATE ASSET DEPRECIATION HELPER METHODS =====

        /// <summary>
        /// Checks if an asset is active (owned) in a specific month
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
        /// Calculates depreciation for a specific asset in a specific month
        /// </summary>
        private decimal CalculateAssetDepreciationForMonth(AssessmentAsset asset, int month)
        {
            // Asset must be depreciable and have positive value
            if (!asset.IsDepreciable || asset.OpeningBalanceValue <= 0 || asset.DepreciationRate == null)
                return 0;

            // Asset must be active in this month
            if (!IsAssetActiveInMonth(asset, month))
                return 0;

            // Apply depreciation method
            if (string.IsNullOrEmpty(asset.DepreciationMethod))
                asset.DepreciationMethod = "Straight-Line"; // Default

            return asset.DepreciationMethod switch
            {
                "Straight-Line" => CalculateStraightLineDepreciation(asset),
                "Declining-Balance" => CalculateDecliningBalanceDepreciation(asset, month),
                "Units-of-Production" => CalculateUnitsOfProductionDepreciation(asset, month),
                _ => CalculateStraightLineDepreciation(asset)
            };
        }

        /// <summary>
        /// Straight-line depreciation: same amount each month
        /// Formula: (Opening Value × Annual Rate %) / 12
        /// </summary>
        private decimal CalculateStraightLineDepreciation(AssessmentAsset asset)
        {
            if (asset.OpeningBalanceValue <= 0 || asset.DepreciationRate == null)
                return 0;

            return (asset.OpeningBalanceValue * (asset.DepreciationRate / 100m)) / 12m;
        }

        /// <summary>
        /// Declining-balance depreciation: higher early on, decreases over time
        /// Formula: (Opening Value × Annual Rate % × 2) / 12
        /// </summary>
        private decimal CalculateDecliningBalanceDepreciation(AssessmentAsset asset, int month)
        {
            if (asset.OpeningBalanceValue <= 0 || asset?.DepreciationRate == null)
                return 0;

            decimal monthlyRate = (asset.DepreciationRate / 100m) * 2m / 12m;
            return asset.OpeningBalanceValue * monthlyRate;
        }

        /// <summary>
        /// Units-of-production depreciation: based on usage
        /// Currently falls back to straight-line (requires production data model)
        /// </summary>
        private decimal CalculateUnitsOfProductionDepreciation(AssessmentAsset asset, int month)
        {
            // TODO: Implement proper units-of-production when production data is available
            return CalculateStraightLineDepreciation(asset);
        }

        /// <summary>
        /// Gets the gross value of an asset for a specific month
        /// Includes any movements (additions, disposals, revaluations)
        /// </summary>
        private decimal GetAssetGrossValueForMonth(AssessmentAsset asset, AssessmentAssetMovement? movement, int month)
        {
            if (movement == null)
                return asset.OpeningBalanceValue;

            decimal grossValue = asset.OpeningBalanceValue;

            // Apply movements up to and including this month
            for (int m = 1; m <= month; m++)
            {
                var movementType = movement.GetMovementType(m);
                var movementValue = movement.GetMovementValue(m);

                if (string.IsNullOrEmpty(movementType) || movementValue == 0)
                    continue;

                grossValue = movementType switch
                {
                    "Addition" => grossValue + movementValue,
                    "Disposal" => grossValue - movementValue,
                    "Revaluation" => grossValue + movementValue,
                    "Transfer" => grossValue, // No impact on gross value
                    _ => grossValue
                };
            }

            return Math.Max(grossValue, 0); // Ensure non-negative
        }

        /// <summary>
        /// Gets accumulated depreciation for an asset at a specific month
        /// Sums all monthly depreciation up to and including that month
        /// </summary>
        private decimal GetAssetAccumulatedDepreciationForMonth(AssessmentAsset asset, AssessmentAssetMovement? movement, int month)
        {
            decimal accumulated = asset.IsPreExisting ? asset.OpeningAccumulatedDepreciation : 0;

            for (int m = 1; m <= month; m++)
            {
                if (IsAssetActiveInMonth(asset, m))
                {
                    accumulated += CalculateAssetDepreciationForMonth(asset, m);
                }
            }

            return accumulated;
        }

        /// <summary>
        /// Retrieves asset movement data
        /// </summary>
        private async Task<AssessmentAssetMovement?> GetAssetMovementAsync(long assessmentAssetId)
        {
            try
            {
                var movements = await _movementRepository.GetAllAsync(m =>
                    m.AssessmentAssetId == assessmentAssetId && m.Active);

                return movements?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving movement data for asset {AssetId}", assessmentAssetId);
                return null;
            }
        }

        #endregion
    }
}