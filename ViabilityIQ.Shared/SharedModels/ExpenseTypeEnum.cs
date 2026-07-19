using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Shared.SharedModels
{
    public enum ExpenseTypeEnum
    {
        ExpenseCategoryA,
        ExpenseCategoryB,
        SundryExpense,
        GrantsDonations
    }

    public class UnifiedExpenseViewModel
    {
        public long Id { get; set; }           // Ensure this line exists
        public string Description { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string ExpenseItemName { get; set; } = string.Empty;
        public long TypeId { get; set; }
        public long ExpenseItemId { get; set; }
        public bool blPercentageOfSalesUsed { get; set; }
        public bool blSendToCashBook { get; set; }
        public decimal[] MonthlyValues { get; set; } = new decimal[12];
    }
}
