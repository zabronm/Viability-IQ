using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{    
    /// Service interface for managing alert dismissals    
    public interface IAlertDismissalService
    {
        /// <summary>
        /// Dismiss an alert and store the dismissal in the database
        /// </summary>
        /// <param name="userId">User ID (long/bigint)</param>
        /// <param name="alertId">Alert ID</param>
        /// <param name="alertType">Type of alert (UrgentAssessment, UpcomingAssessment, Announcement)</param>
        Task<bool> DismissAlertAsync(long userId, string alertId, string alertType);

        /// <summary>
        /// Check if an alert has been dismissed by the user
        /// </summary>
        Task<bool> IsAlertDismissedAsync(long userId, string alertId);

        /// <summary>
        /// Restore a dismissed alert
        /// </summary>
        Task<bool> RestoreDismissedAlertAsync(long userId, string alertId);
    }
}