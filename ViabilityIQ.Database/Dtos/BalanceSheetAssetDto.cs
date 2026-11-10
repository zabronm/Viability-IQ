using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class BalanceSheetAssetDto
    {
        public string AssetCategory { get; set; } = string.Empty;
        public List<BalanceSheetAssetLineDto> Assets { get; set; } = new();

        // Totals
        public decimal TotalGrossValue { get; set; }
        public decimal TotalAccumulatedDepreciation { get; set; }
        public decimal TotalNetValue { get; set; }
    }
}
