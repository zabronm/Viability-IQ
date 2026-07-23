using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Modules;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.FinancialCalculations
{
    internal static class LoanCalculationsEngine
    {
        internal static LoanCalculationResults CalculateReducingBalance(AssessmentLoan loan)
        {
            // Mathematics goes here
            var result = new LoanCalculationResults();
            return result;
        }

        internal static LoanCalculationResults CalculateFlatRate(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

        internal static LoanCalculationResults CalculateInterestOnly(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

        internal static LoanCalculationResults CalculateFixedPrincipal(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

        internal static LoanCalculationResults CalculateBalloon(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }
    }
}
