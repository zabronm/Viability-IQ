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
    [Table("tblAssessmentLoanRepayment")]
    public class AssessmentLoanRepayment : IEntity, ISortableEntity, IAuditableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long AssessmentLoanRepaymentId { get; set; }
        [Required] public long AssessmentId { get; set; }
        public long AssessmentLoanId { get; set;  }

        public long MonthId { get; set; }        
        public decimal ActualRepaymentAmount { get; set; }
        public decimal ExtraRepaymentAmount { get; set; }
        public bool SendToBankAccount { get; set; }
        public bool StartThisMonth { get; set; }        

        public string? Remarks { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy    { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssessmentLoanRepaymentId;
        string ISortableEntity.DisplayName => Remarks;
    }
}
