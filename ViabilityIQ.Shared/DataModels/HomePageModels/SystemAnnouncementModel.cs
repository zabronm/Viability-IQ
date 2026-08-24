using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{
    
    /// Represents a system announcement or notification
    
    public class SystemAnnouncementModel
    {
        
        /// Unique identifier for the announcement
        
        public int Id { get; set; }

        
        /// Title of the announcement
        
        public string Title { get; set; }

        
        /// Full message content
        
        public string Message { get; set; }

        
        /// Type of announcement (Info, Warning, Alert, Maintenance)
        
        public string AnnouncementType { get; set; }

        
        /// When the announcement was created
        
        public DateTime CreatedDate { get; set; }

        
        /// When the announcement expires (optional)
        
        public DateTime? ExpiryDate { get; set; }

        
        /// Whether this announcement is active
        
        public bool IsActive { get; set; }
    }
}
