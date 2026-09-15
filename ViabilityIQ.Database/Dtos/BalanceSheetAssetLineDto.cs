using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class BalanceSheetAssetLineDto
    {
        public string AssetName { get; set; }
        public string AssetType { get; set; }
        public decimal OpeningValue { get; set; }
        public decimal Additions { get; set; }
        public decimal Disposals { get; set; }
        public decimal GrossValue { get; set; }
        public decimal OpeningDepreciation { get; set; }
        public decimal DepreciationCharge { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetValue { get; set; }
    }
}
