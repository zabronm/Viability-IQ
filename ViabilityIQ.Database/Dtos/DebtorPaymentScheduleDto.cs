using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class DebtorPaymentScheduleDto
    {
        public long DebtorPaymentScheduleId { get; set; }
        public long AssessmentId { get; set; }
        public long AssessmentSalesId { get; set; }

        public int SalesMonth { get; set; }
        public int CollectionMonth { get; set; }

        public decimal SalesAmount { get; set; }
        public decimal CollectionAmount { get; set; }
        public decimal PercentageOfSales { get; set; }
        public string? AgeCategory { get; set; }
        public int DaysOutstanding { get; set; }
    }
}
