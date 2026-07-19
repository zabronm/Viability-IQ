using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Shared.SharedModels
{
    public enum StockTypeEnum
    {
        StockCategoryA,
        StockCategoryB,
        SundryStock,
        GrantsDonations
    }

    public class UnifiedStockViewModel
    {
        public long Id { get; set; }           // Ensure this line exists
        public string Description { get; set; } = string.Empty;
        public string AssessmentSalesCategoryName { get; set; } = string.Empty;
        public string StockItemName { get; set; } = string.Empty;
        public long TypeId { get; set; }
        public long IncomeTypeId { get; set; }       
        public bool blIncludeVAT { get; set; }
        public decimal[] MonthlyValues { get; set; } = new decimal[12];
    }
}
