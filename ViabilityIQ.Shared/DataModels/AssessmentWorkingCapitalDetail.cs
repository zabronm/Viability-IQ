using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;


 //====================================================== HOW INVOICE BECOMES CHASH AND WHEN ======================================================== 
 //* 1. Invoice is generated and sent to the customer. The invoice value is recorded in the system.
 //* 2. The customer receives the invoice and processes it for payment. This may involve internal approval processes.
 //* 3. The customer makes a payment based on the invoice terms (e.g., within 30 days).
 //* 4. The payment is received by the business and recorded in the system as cash received.
 //* 5. The cash received is then reflected in the working capital detail, reducing the outstanding balance of the invoice.
 //* 
 //* Note: The timing of when an invoice becomes cash depends on the payment terms agreed upon with the customer and their payment processing speed.
 //*/

namespace ViabilityIQ.Shared.DataModels
{
    [Dapper.Contrib.Extensions.Table("tblAssessmentWorkingCapitalDetail")]
    public class AssessmentWorkingCapitalDetail: IEntity, IAuditableEntity, ISortableEntity
    {
        [Key]  public long WorkingCapitalDetailId { get; set; }
        public long AssessmentId { get; set; }
        public long AssessmentSalesId { get; set; }
        public long ProductCategoryId { get; set; }
        public long IncomeTypeId { get; set; }
        public int SourcePeriod { get; set; }
        public decimal InvoiceValue { get; set; }
        public decimal Debtors_30 { get; set; }
        public decimal Debtors_60 { get; set; }
        public decimal Debtors_90 { get; set; }
        public decimal Debtors_120 { get; set; }
        public decimal Debtors_120Plus { get; set; }
        public decimal ReceiptMonth_1 { get; set; }
        public decimal ReceiptMonth_2 { get; set; }
        public decimal ReceiptMonth_3 { get; set; }
        public decimal ReceiptMonth_4 { get; set; }
        public decimal ReceiptMonth_5 { get; set; }
        public decimal ReceiptMonth_6 { get; set; }
        public decimal ReceiptMonth_7 { get; set; }
        public decimal ReceiptMonth_8 { get; set; }
        public decimal ReceiptMonth_9 { get; set; }
        public decimal ReceiptMonth_10 { get; set; }
        public decimal ReceiptMonth_11 { get; set; }
        public decimal ReceiptMonth_12 { get; set; }
        public decimal OutstandingBalance { get; set; }

        public bool Active { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => WorkingCapitalDetailId;
        public string DisplayName => AssessmentId.ToString();
    }
}
