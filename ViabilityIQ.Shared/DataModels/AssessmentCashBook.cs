using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;


namespace ViabilityIQ.Shared.DataModels
{

    [Table("tblAssessmentCashBook")]
    public class AssessmentCashBook: IEntity, IAuditableEntity, ISortableEntity
    {
        [Key]  public long CashbookDetailId { get; set; }
        public long AssessmentId { get; set; }
        public string TransactionSource { get; set; } = "";
        public long SourceId { get; set; }
        public int Period { get; set; }
        public decimal CashReceipts { get; set; }
        public decimal CashPayments { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public bool Active { get; set; }
        public string? Remarks { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => CashbookDetailId;
        public string DisplayName => AssessmentId.ToString();
    }
}
