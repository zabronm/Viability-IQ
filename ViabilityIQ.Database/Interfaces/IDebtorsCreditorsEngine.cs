using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IDebtorsCreditorsEngine
    {
       
        Task SetConfigurationAsync(long assessmentId, DebtorsConfigurationDto config);       // Configuration
        Task<DebtorsConfigurationDto> GetConfigurationAsync(long assessmentId);

        
        Task GeneratePaymentSchedulesFromSalesAsync(long assessmentId);         // Generate schedules from sales

       
        Task<DebtorsCollectionTableDto> GetCollectionTableAsync(long assessmentId);      // Retrieve data
        Task<List<DebtorsAgingSummaryDto>> GetAgingSummaryAsync(long assessmentId);

       
        Task<decimal> CalculateDaysSalesOutstandingAsync(long assessmentId);             // Metrics
        Task<decimal> CalculateTotalOutstandingAsync(long assessmentId);
        Task<decimal> CalculateTotalBadDebtAsync(long assessmentId);
    }
}