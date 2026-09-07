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
       
        /// Get alerts for a specific user (urgent and upcoming assessments)
       
        Task<AlertsModel> GetAlertsAsync(long userId);

       
        /// Get urgent assessments due within 24 hours
       
        Task<List<UrgentAssessmentModel>> GetUrgentAssessmentsAsync(long userId);

       
        /// Get assessments due this week (1-7 days)
       
        Task<List<UpcomingAssessmentModel>> GetUpcomingAssessmentsAsync(long userId);
    }
}