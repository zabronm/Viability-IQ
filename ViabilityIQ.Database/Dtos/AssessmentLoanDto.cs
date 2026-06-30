using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class AssessmentLoanDto
    {
        public long AssessmentLoanId { get; set; }
        public long AssessmentId { get; set; }
        public DateTime LoanDate { get; set; }
        public string? LonaType { get; set; }
        public string? BankName { get; set; }    
        public decimal LoanAmount { get; set; }
        public decimal LoanBalanceAtAssessmentDate { get; set; }
        public decimal InterestRatePerAnnum { get; set; }
        public int RepaymentPeriodMonths { get; set; }
        public decimal MinimumRepaymentAmount { get; set; }
        public decimal ActualRepaymentAmount { get; set; }
        public bool Active { get; set; } = true;
    }
}
