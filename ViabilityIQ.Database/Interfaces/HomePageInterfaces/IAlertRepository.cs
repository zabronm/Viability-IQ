using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{
    
    /// Repository interface for alert data access    
    public interface IAlertRepository
    {
        /// <summary>
        /// Get alerts for a specific user (urgent and upcoming assessments)
        /// </summary>
        Task<AlertsModel> GetAlertsAsync(long userId);

        /// <summary>
        /// Get urgent assessments due within 24 hours
        /// </summary>
        Task<List<UrgentAssessmentModel>> GetUrgentAssessmentsAsync(long userId);

        /// <summary>
        /// Get assessments due this week (1-7 days)
        /// </summary>
        Task<List<UpcomingAssessmentModel>> GetUpcomingAssessmentsAsync(long userId);
    }
}