using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.FinCalculations
{
    public  class AssessmentFinancialsDto
    {
        // 12-Month Base Arrays (Excluding VAT or Net as configured by application)
        public decimal[] MonthlySales { get; set; } = new decimal[12];
        public decimal[] MonthlyCostOfSales { get; set; } = new decimal[12];
        public decimal[] MonthlySundryIncome { get; set; } = new decimal[12];
        public decimal[] MonthlyExpenses { get; set; } = new decimal[12];

        // Ratios & Performance Context Configuration Inputs
        public decimal TotalFixedCosts { get; set; } // For Break-Even Calculations
        public decimal TotalFixedAssets { get; set; } // For Asset Efficiency (Total Sales / Total Assets)
        public decimal AverageStockValue { get; set; } // For Stock Efficiency/Turnover (COS / Avg Stock)
    }
}
