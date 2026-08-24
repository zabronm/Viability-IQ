using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class DebtorsCollectionTableDto
    {
        public long AssessmentId { get; set; }        
        public List<DebtorSalesRowDto> SalesRows { get; set; } = new();     // All sales rows (one per AssessmentSales record)        
        public DebtorsConfigurationDto Configuration { get; set; }      // Configuration

        // Summary metrics
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalEstimatedBadDebt { get; set; }
        public decimal DaysSalesOutstanding { get; set; }
    }



    
    /// One row in the collection table (represents one AssessmentSales entry)
    
    public class DebtorSalesRowDto
    {
        public long AssessmentSalesId { get; set; }
        public string? Description { get; set; }             // Sales description
        public string? InvoiceMonth { get; set; }            // "Sept-99"
        public decimal InvoicedAmount { get; set; }         // Total from AssessmentSales

        
        public Dictionary<int, decimal> MonthlyCollections { get; set; } = new();       // Collections by month (M1, M2, M3... M12)

        // Row totals
        public decimal TotalCollected { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal EstimatedBadDebt { get; set; }
    }
}
