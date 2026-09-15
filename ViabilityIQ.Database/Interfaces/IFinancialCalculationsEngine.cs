using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Modules;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IFinancialCalculationsEngine
    {
        // ===== LOAN METHODS =====
        LoanCalculationResults CalculateLoan(AssessmentLoan loan, LoanCalculationMethodsEnums method);
        List<AssessmentLoanRepayment> BuildRepaymentRecords(AssessmentLoan loan, LoanCalculationMethodsEnums method);


        // ===== ASSET DEPRECIATION METHODS =====
        Task<decimal> CalculateMonthlyDepreciationAsync(long assessmentId, int month);
        Task<decimal[]> CalculateAnnualDepreciationScheduleAsync(long assessmentId);
        Task<List<AssetDepreciationMonthlyDto>> GetMonthlyDepreciationDetailAsync(long assessmentId, int month);
        Task<AssetDepreciationSummaryDto> GetDepreciationSummaryAsync(long assessmentId);


        // ===== ASSESSMENT INTEGRATION METHODS =====
        Task<bool> RecalculateAssessmentTotalsAsync(long assessmentId);
        Task<bool> RecalculateAssetTotalsAsync(long assessmentId);


    }
}
