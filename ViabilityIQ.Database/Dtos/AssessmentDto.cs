using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Dtos
{
    [Table("vw_assessments_list")]
    public class AssessmentDto
    {
        [Key] public long AssessmentId { get; set; }
        public string? CaseNumber { get; set; }
        public long AssessmentTypeId { get; set; }               //Either Cash Business OR Credit Business; Cash Business does not have Debtors/creditors
        public string? AssessmentType { get; set; }     //Either Cash Business OR Credit Business; Cash Business does not have Debtors/creditors
        public long BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public long ClientId { get; set; }
        public string? BusinessOwner { get; set; }
        public DateTime AssessmentStartDate { get; set; }
        public DateTime AssessmentFinishDate { get; set; }

        //VAT SECTION
        public long VATRate { get; set; }        //Universal VAT rate for the assessment

        //OPENING BALANCES SECTION
        public decimal OpeningBalance_Assets { get; set; }
        public decimal OpeningBalance_Bank { get; set; }
        public decimal InterestOnOverDraft { get; set; } = 0;
        public decimal DepreciationProjectedAmount { get; set; } = 0;        

        //DIRECTORS WAGES SECTION
        public decimal MonthlyDirectorWagesAmountTotal { get; set; }
        public decimal MonthlyDirectorWagesAmount { get; set; }
        public int NumberOfDirectors { get; set; }

        //DEBTORS & CREDITORS SECTION - Creditors
        public decimal Creditors_30 { get; set; }
        public decimal Creditors_60 { get; set; }
        public decimal Creditors_90 { get; set; }
        public decimal Creditors_120 { get; set; }
        public decimal Creditors_120Plus { get; set; }

        //DEBTORS & CREDITORS SECTION - Debtors
        public decimal Debtors_30 { get; set; }
        public decimal Debtors_60 { get; set; }
        public decimal Debtors_90 { get; set; }
        public decimal Debtors_120 { get; set; }
        public decimal Debtors_120Plus { get; set; }

        public long StatusId { get; set; }
        public long ProgressPercentage { get; set; }
        public bool blStock { get; set; }
        public bool blDebtorsCreditors { get; set; }
        public bool blExpenses { get; set; }
        public bool blSales { get; set; }
        public bool blVat { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }
    }
}
