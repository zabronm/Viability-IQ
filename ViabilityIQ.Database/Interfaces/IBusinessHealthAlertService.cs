using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IBusinessHealthAlertService
    {
        /// Service for analyzing cashflow data and generating business health alerts

        /// Generates business health alerts based on cashflow summary           
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="summary">The cashflow summary with KPIs</param>
        /// <returns>List of generated alerts</returns>
        Task<List<BusinessHealthAlert>> GenerateAlertsAsync(long assessmentId, CashflowSummaryDto summary);

        /// Gets the latest alerts for an assessment (max 3, prioritized by severity)           
        /// <param name="assessmentId">The assessment ID</param>
        /// <param name="maxAlerts">Maximum number of alerts to return (default 3)</param>
        /// <returns>List of prioritized alerts</returns>
        Task<List<BusinessHealthAlert>> GetLatestAlertsAsync(long assessmentId, int maxAlerts = 3);

        /// Checks if an assessment has any financial data entered           
        /// <param name="assessmentId">The assessment ID</param>
        /// <returns>True if assessment has sales or expenses</returns>
        Task<bool> HasDataAsync(long assessmentId);

    }
}
