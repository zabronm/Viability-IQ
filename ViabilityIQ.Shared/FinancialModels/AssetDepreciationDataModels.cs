using System;
using System.Collections.Generic;
using System.Linq;

namespace ViabilityIQ.Shared.FinancialModels
{

    /// AssetDepreciationMonthlyDto - Monthly depreciation detail for a specific asset

    public class AssetDepreciationMonthlyDto
    {

        /// The assessment asset ID        
        public long AssessmentAssetId { get; set; }


        /// Asset name for display        
        public string AssetName { get; set; }


        /// The month number (1-12)        
        public int Month { get; set; }


        /// Depreciation method (Straight-Line, Declining-Balance, etc.)

        public string DepreciationMethod { get; set; }


        /// Annual depreciation rate (%)        
        public decimal DepreciationRate { get; set; }


        /// Monthly depreciation expense        
        public decimal MonthlyDepreciation { get; set; }


        /// Gross value at end of month (before accumulated depreciation)        
        public decimal GrossValue { get; set; }


        /// Accumulated depreciation at end of month        
        public decimal AccumulatedDepreciation { get; set; }


        /// Net book value (Gross - Accumulated Depreciation)        
        public decimal NetBookValue { get; set; }


        /// Whether asset is active (owned) in this month        
        public bool IsActive { get; set; }


        /// Display-friendly month label        
        public string MonthDisplay => $"Month {Month}";


        /// Formatted depreciation for display        
        public string FormattedDepreciation => MonthlyDepreciation.ToString("C2");


        /// Formatted gross value for display        
        public string FormattedGrossValue => GrossValue.ToString("C2");


        /// Formatted accumulated depreciation for display        
        public string FormattedAccumulatedDepreciation => AccumulatedDepreciation.ToString("C2");


        /// Formatted net book value for display        
        public string FormattedNetBookValue => NetBookValue.ToString("C2");
    }


    /// AssetDepreciationSummaryDto - Overall depreciation summary for assessment    
    public class AssetDepreciationSummaryDto
    {

        public long AssessmentId { get; set; }
        public int TotalAssetsCount { get; set; }
        public int TotalDepreciableAssetsCount { get; set; }
        public decimal TotalAnnualDepreciation { get; set; }
        public decimal AverageMonthlyDepreciation { get; set; }
        public decimal HighestMonthlyDepreciation { get; set; }
        public decimal LowestMonthlyDepreciation { get; set; }
        public List<decimal> MonthlyDepreciationSchedule { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
        public string FormattedTotalAnnual => TotalAnnualDepreciation.ToString("C2");
        public string FormattedAverageMonthly => AverageMonthlyDepreciation.ToString("C2");
        public decimal DepreciationVariance => HighestMonthlyDepreciation - LowestMonthlyDepreciation;
        public string FormattedVariance => DepreciationVariance.ToString("C2");
        public bool IsConsistentDepreciation => DepreciationVariance < (TotalAnnualDepreciation * 0.1m); // Less than 10% variance

        /// AssetDepreciationReportDto - Detailed report for asset depreciation    
        public class AssetDepreciationReportDto
        {
            public long AssessmentId { get; set; }
            public string ReportTitle => $"Asset Depreciation Schedule - Assessment {AssessmentId}";
            public AssetDepreciationSummaryDto Summary { get; set; }


            /// Detailed depreciation by asset by month
            /// Dictionary: Month -> List of Asset Depreciation Details        
            public Dictionary<int, List<AssetDepreciationMonthlyDto>> DetailByMonth { get; set; } = new();
            public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;


            /// Gets total depreciation for a specific month        
            public decimal GetMonthTotal(int month)
            {
                if (!DetailByMonth.TryGetValue(month, out var monthDetails))
                    return 0;

                return monthDetails.Sum(d => d.MonthlyDepreciation);
            }


            /// Gets total gross value for a specific month        
            public decimal GetMonthTotalGrossValue(int month)
            {
                if (!DetailByMonth.TryGetValue(month, out var monthDetails))
                    return 0;

                return monthDetails.Sum(d => d.GrossValue);
            }


            /// Gets total accumulated depreciation for a specific month        
            public decimal GetMonthTotalAccumulatedDepreciation(int month)
            {
                if (!DetailByMonth.TryGetValue(month, out var monthDetails))
                    return 0;

                return monthDetails.Sum(d => d.AccumulatedDepreciation);
            }


            /// Gets total net book value for a specific month        
            public decimal GetMonthTotalNetBookValue(int month)
            {
                if (!DetailByMonth.TryGetValue(month, out var monthDetails))
                    return 0;

                return monthDetails.Sum(d => d.NetBookValue);
            }
        }


        /// AssetDepreciationByMethodDto - Breakdown of depreciation by method    
        public class AssetDepreciationByMethodDto
        {
            public string DepreciationMethod { get; set; }

            public int AssetCount { get; set; }

            /// Total depreciation using this method
            public decimal TotalDepreciation { get; set; }

            /// Monthly depreciation values for this method
            public List<decimal> MonthlyValues { get; set; } = new();
            /// Average monthly depreciation for this method

            public decimal AverageMonthly => MonthlyValues.Count > 0 ? MonthlyValues.Average() : 0;

            /// Percentage of total depreciation        
            public decimal PercentageOfTotal { get; set; }

            /// Formatted total depreciation        
            public string FormattedTotal => TotalDepreciation.ToString("C2");

            /// Formatted percentage        
            public string FormattedPercentage => PercentageOfTotal.ToString("0.00%");
        }
    }
}
