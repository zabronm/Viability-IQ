using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class AssetTypeDto
    {
        public long AssetTypeId { get; set; }
        public long AssetCategoryId { get; set; }
        public string TypeName { get; set; }
        public string? AssetCategoryName { get; set; }  // For display
        public decimal DefaultDepreciationRate { get; set; }
        public int? DefaultUsefulLifeYears { get; set; }
        public bool IsDepreciable { get; set; }

        // Computed
        public string DepreciationInfo => "10% p.a. (10 years)";
    }
}
