using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class DebtorsAgingSummaryDto
    {
        public int Month { get; set; }                     // 1-12
        public decimal Current { get; set; }               // 0-30 days
        public decimal Debtors_30 { get; set; }
        public decimal Debtors_60 { get; set; }
        public decimal Debtors_90 { get; set; }
        public decimal Debtors_120 { get; set; }
        public decimal Debtors_120Plus { get; set; }

        public decimal TotalOutstanding { get; set; }
    }
}
