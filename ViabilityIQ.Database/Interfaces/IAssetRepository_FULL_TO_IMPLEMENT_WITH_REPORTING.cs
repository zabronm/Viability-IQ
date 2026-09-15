using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces
{
    /// <summary>
    /// IAssetRepository - Specialized repository for Assessment Assets and their monthly movements
    /// Supports loading, saving, and querying asset data with depreciation calculations
    /// Integrates with cashflow engine for financial projections
    /// </summary>
    public interface IAssetRepository_FULL_TO_IMPLEMENT_WITH_REPORTING
    {

        #region Asset Master Data Operations

        /// <summary>
        /// Gets all assets for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="includeInactive">Whether to include inactive assets (default: false)</param>
        /// <returns>List of assessment assets</returns>
        Task<List<AssessmentAsset>> GetAssessmentAssetsAsync(long assessmentId, bool includeInactive = false);

        /// <summary>
        /// Gets a single asset by ID
        /// </summary>
        /// <param name="assetId">The assessment asset ID</param>
        /// <returns>The assessment asset or null if not found</returns>
        Task<AssessmentAsset> GetAssetByIdAsync(long assetId);

        /// <summary>
        /// Gets assets of a specific type for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="assetTypeId">The asset type ID to filter by</param>
        /// <returns>List of assets matching the type</returns>
        Task<List<AssessmentAsset>> GetAssetsByTypeAsync(long assessmentId, long assetTypeId);

        /// <summary>
        /// Gets only depreciable assets for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>List of depreciable assets</returns>
        Task<List<AssessmentAsset>> GetDepreciableAssetsAsync(long assessmentId);

        /// <summary>
        /// Gets only tangible assets for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>List of tangible assets</returns>
        Task<List<AssessmentAsset>> GetTangibleAssetsAsync(long assessmentId);

        /// <summary>
        /// Gets assets acquired during a specific period
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="startMonth">Start month (1-12)</param>
        /// <param name="endMonth">End month (1-12)</param>
        /// <returns>List of assets acquired in the period</returns>
        Task<List<AssessmentAsset>> GetAcquiredAssetsInPeriodAsync(long assessmentId, int startMonth, int endMonth);

        /// <summary>
        /// Saves an asset (creates or updates)
        /// </summary>
        /// <param name="asset">The asset to save</param>
        /// <returns>The saved asset ID</returns>
        Task<long> SaveAssetAsync(AssessmentAsset asset);

        /// <summary>
        /// Deletes an asset (soft delete - marks as inactive)
        /// </summary>
        /// <param name="assetId">The asset ID to delete</param>
        /// <param name="userId">The user ID performing the deletion</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAssetAsync(long assetId, long userId);

        /// <summary>
        /// Bulk saves multiple assets
        /// </summary>
        /// <param name="assets">The assets to save</param>
        /// <returns>Number of assets saved</returns>
        Task<int> BulkSaveAssetsAsync(List<AssessmentAsset> assets);

        #endregion

        #region Asset Movement Operations

        /// <summary>
        /// Gets monthly movements for an asset
        /// </summary>
        /// <param name="assetId">The assessment asset ID</param>
        /// <returns>The movement record or null if not found</returns>
        Task<AssessmentAssetMovement> GetAssetMovementsAsync(long assetId);

        /// <summary>
        /// Gets movements for all assets in an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>List of movement records</returns>
        Task<List<AssessmentAssetMovement>> GetAssessmentMovementsAsync(long assessmentId);

        /// <summary>
        /// Saves asset monthly movements
        /// </summary>
        /// <param name="movement">The movement record to save</param>
        /// <returns>The saved movement ID</returns>
        Task<long> SaveAssetMovementsAsync(AssessmentAssetMovement movement);

        /// <summary>
        /// Bulk saves multiple movement records
        /// </summary>
        /// <param name="movements">The movement records to save</param>
        /// <returns>Number of records saved</returns>
        Task<int> BulkSaveMovementsAsync(List<AssessmentAssetMovement> movements);

        /// <summary>
        /// Deletes asset movements (soft delete)
        /// </summary>
        /// <param name="assetId">The asset ID</param>
        /// <param name="userId">The user ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAssetMovementsAsync(long assetId, long userId);

        /// <summary>
        /// Gets movement data for a specific month across all assets
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>List of movement records for that month</returns>
        Task<List<AssessmentAssetMovement>> GetMovementsByMonthAsync(long assessmentId, int month);

        #endregion

        #region Asset Valuation & Depreciation

        /// <summary>
        /// Calculates total asset value for an assessment at a specific month
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Total gross value of assets</returns>
        Task<decimal> GetTotalAssetValueAtMonthAsync(long assessmentId, int month);

        /// <summary>
        /// Calculates total accumulated depreciation for an assessment at a specific month
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Total accumulated depreciation</returns>
        Task<decimal> GetTotalAccumulatedDepreciationAtMonthAsync(long assessmentId, int month);

        /// <summary>
        /// Calculates total net book value for an assessment at a specific month
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Total net book value (Gross - Accumulated Depreciation)</returns>
        Task<decimal> GetTotalNetBookValueAtMonthAsync(long assessmentId, int month);

        /// <summary>
        /// Calculates total monthly depreciation expense for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Total depreciation expense for the month</returns>
        Task<decimal> GetTotalDepreciationAtMonthAsync(long assessmentId, int month);

        /// <summary>
        /// Gets depreciation schedule for an asset
        /// </summary>
        /// <param name="assetId">The asset ID</param>
        /// <returns>Array of 12 monthly depreciation amounts</returns>
        Task<decimal[]> GetDepreciationScheduleAsync(long assetId);

        /// <summary>
        /// Calculates accumulated depreciation for an asset at a specific month
        /// </summary>
        /// <param name="assetId">The asset ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Accumulated depreciation value</returns>
        Task<decimal> GetAssetAccumulatedDepreciationAtMonthAsync(long assetId, int month);

        /// <summary>
        /// Calculates net book value for an asset at a specific month
        /// </summary>
        /// <param name="assetId">The asset ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Net book value at the month</returns>
        Task<decimal> GetAssetNetBookValueAtMonthAsync(long assetId, int month);

        #endregion

        #region Asset Summary & Reporting

        /// <summary>
        /// Gets comprehensive asset summary for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>Asset summary with totals and metrics</returns>
        Task<AssetSummary> GetAssetSummaryAsync(long assessmentId);

        /// <summary>
        /// Gets asset report data for financial reporting
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>Asset report data</returns>
        Task<AssetReport> GetAssetReportAsync(long assessmentId);

        /// <summary>
        /// Gets opening balances for all assets (for balance sheet)
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>Opening asset schedule</returns>
        Task<List<AssetOpeningBalance>> GetOpeningBalancesAsync(long assessmentId);

        /// <summary>
        /// Gets closing balances for all assets at a specific month
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="month">The month number (1-12)</param>
        /// <returns>Closing asset schedule</returns>
        Task<List<AssetClosingBalance>> GetClosingBalancesAsync(long assessmentId, int month);

        #endregion

        #region Cache & Invalidation

        /// <summary>
        /// Clears all asset-related caches for an assessment
        /// </summary>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>True if successful</returns>
        Task<bool> ClearAssetCacheAsync(long assessmentId);

        /// <summary>
        /// Invalidates calculation caches when asset data changes
        /// </summary>
        /// <param name="assetId">The asset ID that changed</param>
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>True if successful</returns>
        Task<bool> InvalidateAssetCalculationsAsync(long assetId, long assessmentId);

        #endregion
    }

    #region Supporting Models for Reporting

    /// <summary>
    /// Asset summary with calculated totals
    /// </summary>
    public class AssetSummary
    {
        public long AssessmentId { get; set; }
        public int TotalAssetCount { get; set; }
        public int DepreciableAssetCount { get; set; }
        public int TangibleAssetCount { get; set; }
        public decimal OpeningGrossValue { get; set; }
        public decimal OpeningAccumulatedDepreciation { get; set; }
        public decimal OpeningNetBookValue { get; set; }
        public decimal ClosingGrossValue { get; set; }
        public decimal ClosingAccumulatedDepreciation { get; set; }
        public decimal ClosingNetBookValue { get; set; }
        public decimal TotalDepreciationExpense { get; set; }
        public decimal TotalMovementValue { get; set; }
        public int AssetsAcquiredDuringPeriod { get; set; }
        public int AssetsDisposedDuringPeriod { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    /// <summary>
    /// Detailed asset report for financial reporting
    /// </summary>
    public class AssetReport
    {
        public long AssessmentId { get; set; }
        public List<AssetLineItem> Assets { get; set; } = new();
        public AssetReportTotals Totals { get; set; } = new();
        public List<AssetMovementSummary> Movements { get; set; } = new();
        public DateTime ReportDate { get; set; }
    }

    /// <summary>
    /// Individual asset line for report
    /// </summary>
    public class AssetLineItem
    {
        public long AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetType { get; set; }
        public decimal OpeningGrossValue { get; set; }
        public decimal OpeningAccumulatedDepreciation { get; set; }
        public decimal OpeningNetValue { get; set; }
        public decimal MovementValue { get; set; }
        public string MovementType { get; set; }
        public decimal ClosingGrossValue { get; set; }
        public decimal ClosingAccumulatedDepreciation { get; set; }
        public decimal ClosingNetValue { get; set; }
        public decimal TotalDepreciation { get; set; }
        public int AcquisitionMonth { get; set; }
    }

    /// <summary>
    /// Report totals
    /// </summary>
    public class AssetReportTotals
    {
        public decimal TotalOpeningGross { get; set; }
        public decimal TotalOpeningAccumulated { get; set; }
        public decimal TotalOpeningNet { get; set; }
        public decimal TotalMovements { get; set; }
        public decimal TotalClosingGross { get; set; }
        public decimal TotalClosingAccumulated { get; set; }
        public decimal TotalClosingNet { get; set; }
        public decimal TotalDepreciationExpense { get; set; }
    }

    /// <summary>
    /// Movement summary for report
    /// </summary>
    public class AssetMovementSummary
    {
        public string MovementType { get; set; }
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public List<long> AssetIds { get; set; } = new();
    }

    /// <summary>
    /// Opening balance schedule
    /// </summary>
    public class AssetOpeningBalance
    {
        public long AssetId { get; set; }
        public string AssetName { get; set; }
        public decimal GrossValue { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetBookValue { get; set; }
        public decimal DepreciationRate { get; set; }
        public string DepreciationMethod { get; set; }
    }

    /// <summary>
    /// Closing balance schedule
    /// </summary>
    public class AssetClosingBalance
    {
        public long AssetId { get; set; }
        public string AssetName { get; set; }
        public int Month { get; set; }
        public decimal GrossValue { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetBookValue { get; set; }
        public decimal MonthDepreciation { get; set; }
        public string LastMovementType { get; set; }
    }

    #endregion
}
