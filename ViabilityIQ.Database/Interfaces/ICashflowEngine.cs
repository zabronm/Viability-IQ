using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.FinancialModels;


namespace ViabilityIQ.Application.Interfaces
{
    public interface ICashflowEngine
    {
      
        /// Calculates cashflow for all 12 months      
        Task<List<AssessmentCashflow>> CalculateMonthlyCashflowAsync(long assessmentId);

      
        /// Calculates cashflow summary for the entire year      
        Task<CashflowSummary> CalculateCashflowSummaryAsync(long assessmentId);

      
        /// Gets monthly cashflow DTOs for display      
        Task<List<CashflowMonthlyDto>> GetMonthlyCashflowDisplayAsync(long assessmentId);

      
        /// Gets cashflow summary DTO for display      
        Task<CashflowSummaryDto> GetCashflowSummaryDisplayAsync(long assessmentId);

      
        /// Recalculates cashflow when underlying data changes      
        Task<bool> RecalculateCashflowAsync(long assessmentId);
    }
}
