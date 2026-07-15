using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblAssessmentVATTransactions")]
    public class AssessmentVATTransactions : IEntity, IAuditableEntity, ISortableEntity
    {
        [Key]  public long VATTransactionId { get; set; }
        public long AssessmentId { get; set; }
        public long SourceId { get; set; }
        public string SourceType { get; set; } = "";
        public int Period { get; set; }
        public decimal VATRate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal OutputVAT { get; set; }
        public decimal InputVAT { get; set; }
        public decimal VATPayable { get; set; }

        public bool Active { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => VATTransactionId;
        public string DisplayName => AssessmentId.ToString();


    }
}

