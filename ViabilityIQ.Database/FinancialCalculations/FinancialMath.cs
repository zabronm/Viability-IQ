using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.FinancialCalculations
{
    public static class FinancialMath
    {
        
        /// Calculates the fixed monthly repayment (PMT).
        
        public static decimal CalculateMonthlyRepayment(decimal principal, decimal annualRate, int months)
        {
            if (months <= 0) return 0;

            if (annualRate == 0) return Math.Round(principal / months, 2);

            decimal monthlyRate = annualRate / 12m / 100m;

            double r = (double)monthlyRate;
            double p = (double)principal;

            double payment = p * (r * Math.Pow(1 + r, months)) / (Math.Pow(1 + r, months) - 1);

            return Math.Round((decimal)payment, 2);
        }
    }
}
