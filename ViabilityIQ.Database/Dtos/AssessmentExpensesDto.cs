using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;


namespace ViabilityIQ.Shared.DataModels
{
    [Dapper.Contrib.Extensions.Table("vw_assessment_expenses_list")]
    public class AssessmentExpensesDto 
    {
        [Key] public int AssessmentExpensesId { get; set; }       
        public decimal AssessmentId { get; set; }
        public decimal ProductCategoryId { get; set; }
        public long ExpenseTypeId { get; set; }
        public string? ExpenseTypeName { get; set; }
        public long ExpenseItemId { get; set; }
        public string? ExpenseItemName { get; set; }
        public bool blSendToCashBook { get; set; }
        public bool blPercentageOfSalesUsed { get; set; } 
        public decimal PercentageOfSalesRate { get; set;  }
        public string?  Description { get; set; }   
        public decimal SameMonthlyAmountUsed { get; set; }
        public decimal SameMonthlyAmount { get; set; }
        public decimal Month_1 { get; set; }
        public decimal Month_2 { get; set; }
        public decimal Month_3 { get; set; }
        public decimal Month_4 { get; set; }
        public decimal Month_5 { get; set; }
        public decimal Month_6 { get; set; }
        public decimal Month_7 { get; set; }
        public decimal Month_8 { get; set; }
        public decimal Month_9 { get; set; }
        public decimal Month_10 { get; set; }
        public decimal Month_11 { get; set; }
        public decimal Month_12 { get; set; }
        public decimal TotalNoVAT { get; set; }
        public decimal TotalWithVAT { get; set; } 
    }
}
