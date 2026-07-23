using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Modules;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IFinancialCalculationsEngine
    {
        LoanCalculationResults CalculateLoan(AssessmentLoan loan, LoanCalculationMethodsEnums method);
        List<AssessmentLoanRepayment> BuildRepaymentRecords(AssessmentLoan loan, LoanCalculationMethodsEnums method);


    }
}
