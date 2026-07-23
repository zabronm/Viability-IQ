using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Modules;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;


namespace ViabilityIQ.Application.FinancialCalculations
{
    public class FinancialCalculationsEngine : IFinancialCalculationsEngine
    {
        public LoanCalculationResults CalculateLoan(AssessmentLoan loan, LoanCalculationMethodsEnums method)
        {
            return method switch
            {
                LoanCalculationMethodsEnums.ReducingBalance => CalculateReducingBalance(loan),
                LoanCalculationMethodsEnums.FlatRate => CalculateFlatRate(loan),
                LoanCalculationMethodsEnums.InterestOnly => CalculateInterestOnly(loan),
                LoanCalculationMethodsEnums.FixedPrincipal => CalculateFixedPrincipal(loan),
                LoanCalculationMethodsEnums.Balloon => CalculateBalloon(loan),

                _ => throw new NotSupportedException()
            };
        }


        //==============  calculate loan amounts based on the REDUCING BALANCE METHOD =============
        private LoanCalculationResults CalculateReducingBalance(AssessmentLoan loan)
        {
            try
            {
                LoanCalculationResults result = new();

                decimal balance = loan.LoanBalanceAtAssessmentDate;
                decimal payment = FinancialMath.CalculateMonthlyRepayment(balance, loan.InterestRatePerAnnum, loan.RepaymentPeriodMonths);

                result.MonthlyRepayment = payment;
                decimal monthlyRate = loan.InterestRatePerAnnum / 12m / 100m;

                int startIndex = Math.Max(0, loan.StartMonth - 1);
                int repaymentMonths = Math.Min(loan.RepaymentPeriodMonths, 12 - startIndex);

                for (int i = 0; i < repaymentMonths; i++)
                {
                    decimal interest = Math.Round(balance * monthlyRate, 2);
                    decimal principal = payment - interest;

                    if (principal > balance) principal = balance;
                    balance -= principal;

                    result.ExpectedRepayment[startIndex + i] = payment;
                    result.Interest[startIndex + i] = interest;
                    result.Principal[startIndex + i] = principal;
                    result.OutstandingBalance[startIndex + i] = Math.Max(balance, 0);
                    result.ExtraRepayment[startIndex + i] = 0;
                }

                return result;

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public List<AssessmentLoanRepayment> BuildRepaymentRecords(AssessmentLoan loan, LoanCalculationMethodsEnums method)
        {
            var calculation = CalculateLoan(loan, method);
            var rows = new List<AssessmentLoanRepayment>();

            rows.Add(CreateRepaymentRow(loan, 1, calculation.ExpectedRepayment));    // Expected repayment           
            rows.Add(CreateRepaymentRow(loan, 2, calculation.Interest));             // Interest           
            rows.Add(CreateRepaymentRow(loan, 3, calculation.ExtraRepayment));       // Extra repayment

            return rows;
        }


        private AssessmentLoanRepayment CreateRepaymentRow(AssessmentLoan loan, int metricTypeId, decimal[] values)
        {
            var row = new AssessmentLoanRepayment
            {
                AssessmentId = loan.AssessmentId,
                AssessmentLoanId = loan.AssessmentLoanId,
                MetricTypeId = metricTypeId,
                AssessmentLoanRepaymentId = 0,                      //======== NEW RECORD HERE ============
                MonthlyValues = values,
                Active = true,                                     //======== EXTREMELY IMPORTANT OTHERWISE RECORD WILL NOT USED IN TRANSACTIONS ============                            
            };

            return row;
        }



        private LoanCalculationResults CalculateFlatRate(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

        private LoanCalculationResults CalculateInterestOnly(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

        private LoanCalculationResults CalculateFixedPrincipal(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

        private LoanCalculationResults CalculateBalloon(AssessmentLoan loan)
        {
            throw new NotImplementedException();
        }

    }
}
