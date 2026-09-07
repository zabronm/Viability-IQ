using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class AssetSummaryDto
    {
        public int TotalAssetCount { get; set; }
        public decimal TotalGrossValue { get; set; }
        public decimal TotalAccumulatedDepreciation { get; set; }
        public decimal TotalNetValue { get; set; }

        // Asset breakdown
        public int FixedAssetCount { get; set; }
        public decimal FixedAssetValue { get; set; }
        public decimal FixedAssetPercentage { get; set; }

        public int CurrentAssetCount { get; set; }
        public decimal CurrentAssetValue { get; set; }
        public decimal CurrentAssetPercentage { get; set; }

        // Health metrics
        public int ImpairedAssetCount { get; set; }
        public int FullyDepreciatedCount { get; set; }
        public decimal AverageDepreciationPercentage { get; set; }
        public decimal HealthScore { get; set; }  // 0-100%
        public string HealthStatus { get; set; }  // "Excellent", "Good", "Fair", "Poor", "Critical"
    }
}
