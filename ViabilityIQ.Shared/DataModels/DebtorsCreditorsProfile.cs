using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblAssessmentDebtorsCreditorsProfile")]
    public class DebtorsCreditorsProfile : IEntity, IAuditableEntity, ISortableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long DebtorsCreditorsProfileId { get; set; }
        public bool EntryMode { get; set; }
        public long AssessmentId { get; set; }
        [Required]
        [Range(0, 100, ErrorMessage = "Must be between zero(0) and 100")]
        public decimal Creditors_30 { get; set; }
        public decimal Creditors_60 { get; set; }
        public decimal Creditors_90 { get; set; }
        public decimal Creditors_120 { get; set; }
        public decimal Creditors_120Plus { get; set; }

        //CREDITORS SECTION - Creditors - Vaues
        public decimal CreditorsValue_30 { get; set; }
        public decimal CreditorsValue_60 { get; set; }
        public decimal CreditorsValue_90 { get; set; }
        public decimal CreditorsValue_120 { get; set; }
        public decimal CreditorsValue_120Plus { get; set; }


        //=== Additional very important fields for Debtors and Creditors ===
        public decimal BadDebtPercentage { get; set; } = 2m;
        public int AveragePaymentDays { get; set; }
        public bool IncludeVAT { get; set; }

        //DEBTORS SECTION - Debtors => Percentages
        [Required]
        [Range(0, 100, ErrorMessage = "Must be between zero(0) and 100.")]
        public decimal Debtors_30 { get; set; }
        public decimal Debtors_60 { get; set; }
        public decimal Debtors_90 { get; set; }
        public decimal Debtors_120 { get; set; }
        public decimal Debtors_120Plus { get; set; }

        //DEBTORS SECTION - Debtors => Values
        public decimal DebtorsValue_30 { get; set; }
        public decimal DebtorsValue_60 { get; set; }
        public decimal DebtorsValue_90 { get; set; }
        public decimal DebtorsValue_120 { get; set; }
        public decimal DebtorsValue_120Plus { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }
        public bool Active { get; set; }
        public string? Remarks { get; set; }
               
        long IEntity.Id => DebtorsCreditorsProfileId;
        string ISortableEntity.DisplayName => AssessmentId.ToString();
    }
}
