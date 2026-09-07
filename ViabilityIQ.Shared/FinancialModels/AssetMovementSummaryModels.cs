using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.FinancialModels
{
    public class AssetMovementMonthlySummaryDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public string MovementType { get; set; }
        public decimal MovementValue { get; set; }
        public decimal GrossValue { get; set; }
        public decimal Depreciation { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetBookValue { get; set; }
        public bool IsAssetActive { get; set; }

        // Display helpers
        public string FormattedMovementValue => MovementValue.ToString("C2");
        public string FormattedGrossValue => GrossValue.ToString("C2");
        public string FormattedDepreciation => Depreciation.ToString("C2");
        public string FormattedAccumulatedDepreciation => AccumulatedDepreciation.ToString("C2");
        public string FormattedNetBookValue => NetBookValue.ToString("C2");
        public string StatusIcon => IsAssetActive ? "✓" : "✗";
    }

    /// <summary>
    /// Annual summary for an asset
    /// </summary>
    public class AssetMovementAnnualSummaryDto
    {
        public long AssetId { get; set; }
        public string AssetName { get; set; }

        // Opening values
        public decimal OpeningGrossValue { get; set; }
        public decimal OpeningAccumulatedDepreciation { get; set; }
        public decimal OpeningNetBookValue { get; set; }

        // Closing values (end of year)
        public decimal ClosingGrossValue { get; set; }
        public decimal ClosingAccumulatedDepreciation { get; set; }
        public decimal ClosingNetBookValue { get; set; }

        // Annual totals
        public decimal TotalDepreciation { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDisposals { get; set; }
        public decimal TotalRevaluations { get; set; }

        // Calculated properties
        public decimal TotalMovements => TotalAdditions + TotalDisposals + TotalRevaluations;
        public decimal DepreciationPercentage => OpeningGrossValue > 0
            ? (TotalDepreciation / OpeningGrossValue) * 100m
            : 0;

        // Display helpers
        public string FormattedOpeningGross => OpeningGrossValue.ToString("C2");
        public string FormattedClosingGross => ClosingGrossValue.ToString("C2");
        public string FormattedTotalDepreciation => TotalDepreciation.ToString("C2");
        public string FormattedClosingNBV => ClosingNetBookValue.ToString("C2");
    }
}

