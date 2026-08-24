using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{
    
    /// Represents a single activity log entry displayed on the dashboard
    
    public class ActivityLogModel
    {
        
        /// Unique identifier for the activity
        
        public string Id { get; set; }

        
        /// Name of the user who performed the action
        
        public string ActorName { get; set; }

        
        /// Type of activity (View, Edit, Approve, Delete, Create, Update, Share)
        
        public string ActivityType { get; set; }

        
        /// Name of the object being acted upon (e.g., assessment name)
        
        public string ObjectName { get; set; }

        
        /// When the activity occurred
        
        public DateTime CreatedDate { get; set; }

        
        /// Optional link to navigate to the related object
        
        public string NavigationUrl { get; set; }
    }
}