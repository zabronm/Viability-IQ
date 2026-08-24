using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.HomePageModels;


namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{
    
    /// Repository interface for system announcement data access    
    public interface IAnnouncementRepository
    {        
        /// Get active system announcements        
        Task<List<SystemAnnouncementModel>> GetActiveAnnouncementsAsync();
        
        /// Get announcements not dismissed by the user        
        Task<List<SystemAnnouncementModel>> GetNonDismissedAnnouncementsAsync(long userId);
        
        /// Check if an announcement has been dismissed by the user        
        Task<bool> IsAnnouncementDismissedAsync(long userId, int announcementId);
    }
}