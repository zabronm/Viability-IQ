using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Modules
{
    public class LoanCalculationResults
    {
        public decimal MonthlyRepayment { get; set; }
        public decimal[] ExpectedRepayment { get; set; } = new decimal[12];
        public decimal[] Interest { get; set; } = new decimal[12];
        public decimal[] Principal { get; set; } = new decimal[12];
        public decimal[] ExtraRepayment { get; set; } = new decimal[12];
        public decimal[] OutstandingBalance { get; set; } = new decimal[12];

    }
}
