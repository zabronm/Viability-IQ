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
    [Dapper.Contrib.Extensions.Table("vw_assessment_sales_list")]
    public class AssessmentSalesDto 
    {
        [Key] public int AssessmentSalesId { get; set; }       
        public decimal AssessmentId { get; set; }
        public decimal ProductCategoryId { get; set; }
        public decimal IncomeTypeId { get; set; }
        public string? IncomeTypeName { get; set; }
        public string?  Description { get; set; }
        public decimal IncludeVAT { get; set; }
        public decimal VATRate { get; set; }
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
