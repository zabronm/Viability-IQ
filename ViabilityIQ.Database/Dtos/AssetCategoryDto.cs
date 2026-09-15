using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class AssetCategoryDto
    {
        public long AssetCategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsCurrentAsset { get; set; }
        public string? BalanceSheetSection { get; set; }

        // Computed
        public string CategoryType => IsCurrentAsset ? "Current Asset" : "Fixed Asset";
    }
}
