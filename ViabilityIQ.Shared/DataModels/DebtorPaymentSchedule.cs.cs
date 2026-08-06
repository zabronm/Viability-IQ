using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;


namespace ViabilityIQ.Shared.DataModels
{
    [Dapper.Contrib.Extensions.Table("tblDebtorPaymentSchedule")]
    public class DebtorPaymentSchedule : IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long DebtorPaymentScheduleId { get; set; }
        public long AssessmentId { get; set; }
        public long AssessmentSalesId { get; set; }           // Link to source sales       
        public int SalesMonth { get; set; }                    // // Which month's sales this is from 1-12 (M1, M2, etc.)       
        public int CollectionMonth { get; set; }               // // When the collection happens 1-12 (M1, M2, etc.)       
        public decimal SalesAmount { get; set; }             // Sales amount for that month       
        public decimal CollectionAmount { get; set; }          // // Calculated collection What's collected in this month

        // Metadata
        public decimal PercentageOfSales { get; set; }         // 50%, 20%, etc.
        public string? AgeCategory { get; set; }                // "0-30 days", "30-60 days", etc.
        public int DaysOutstanding { get; set; }               // ~15, ~45, ~75, ~105, ~150


        public bool Active { get; set; } = true;
        public string? Remarks { get; set; } = string.Empty;


        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => DebtorPaymentScheduleId;
        string ISortableEntity.DisplayName => AssessmentId.ToString();
    }
}
