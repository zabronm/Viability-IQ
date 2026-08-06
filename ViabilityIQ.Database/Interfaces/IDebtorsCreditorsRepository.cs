using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IDebtorsCreditorsRepository
    {
        // Configuration
        Task<DebtorsCreditorsProfile> GetConfigurationAsync(long assessmentId);
        Task<long> SetConfigurationAsync(DebtorsCreditorsProfile config);      
        Task AddPaymentSchedulesAsync(List<DebtorPaymentSchedule> schedules);             // Payment Schedules
        Task<List<DebtorPaymentSchedule>> GetPaymentSchedulesAsync(long assessmentId);
        Task DeletePaymentSchedulesAsync(long assessmentId);        
        Task<List<AssessmentSales>> GetAssessmentSalesAsync(long assessmentId);     // Assessment Sales
    }
}
