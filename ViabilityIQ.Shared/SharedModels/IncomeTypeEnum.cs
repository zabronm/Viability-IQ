using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Shared.SharedModels
{
    public enum IncomeTypeEnum
    {
        SalesCategoryA,
        SalesCategoryB,
        SundryIncome,
        GrantsDonations
    }

    public class UnifiedIncomeViewModel
    {
        public long Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public IncomeTypeEnum Type { get; set; }
        public bool IncludesVat { get; set; }
        public decimal[] MonthlyValues { get; set; } = new decimal[12];

    }
}
