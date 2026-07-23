using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using ViabilityIQ.Shared.DataModelsInterfaces;


namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblAssessmentLoan")]
    public class AssessmentLoan : IEntity, ISortableEntity, IAuditableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long AssessmentLoanId { get; set; }       
        [Required] public long LoanTypeId{ get; set; }
        [Required] public long AssessmentId { get; set; }

        public long BankId { get; set; }
        public DateTime? LoanDate { get; set; } = null;
        public decimal LoanAmount { get; set; }
        public decimal LoanBalanceAtAssessmentDate { get; set; }
        public decimal InterestRatePerAnnum { get; set; }
        public int RepaymentPeriodMonths { get; set; }
        public decimal MinimumRepaymentAmount { get; set; }
        public decimal ActualRepaymentAmount {get; set;}

        [Required(ErrorMessage ="First repayment month is required")]
        public int StartMonth { get; set; }

        public string? Remarks { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy    { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssessmentLoanId;
        string ISortableEntity.DisplayName => Remarks;
    }
}
