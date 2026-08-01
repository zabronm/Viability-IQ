using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface ICashflowRepository
    {
        Task SaveMonthlyCashflowAsync(List<AssessmentCashflow> cashflows);
        Task SaveCashflowSummaryAsync(CashflowSummary summary);
        Task<List<AssessmentCashflow>> GetMonthlyCashflowAsync(long assessmentId);
        Task<CashflowSummary> GetCashflowSummaryAsync(long assessmentId);
        Task ClearCashflowAsync(long assessmentId);
    }
}
