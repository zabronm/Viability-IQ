using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class DebtorsConfigurationDto
    {
        public long DebtorsCreditorsProfileId { get; set; }           //Please note that this comes from table WorkingCapitalProfile
        public long AssessmentId { get; set; }
        public decimal Debtors_30 { get; set; }
        public decimal Debtors_60 { get; set; }
        public decimal Debtors_90 { get; set; }
        public decimal Debtors_120 { get; set; }
        public decimal Debtors_120Plus { get; set; }

        public decimal BadDebtPercentage { get; set; }
        public int AveragePaymentDays { get; set; }
        public bool IncludeTax { get; set; } = false;
    }
}
