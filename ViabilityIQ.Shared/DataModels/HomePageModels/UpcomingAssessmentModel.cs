using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{
    
    /// Represents an assessment due this week (1-7 days)    
    public class UpcomingAssessmentModel
    {
       
        /// Unique identifier for the alert
       
        public string Id { get; set; }

       
        /// The assessment ID (long/bigint)
       
        public long AssessmentId { get; set; }

       
        /// Name of the assessment
       
        public string AssessmentName { get; set; }

       
        /// The business this assessment is for
       
        public long BusinessId { get; set; }

       
        /// Name of the business being assessed
       
        public string BusinessName { get; set; }

       
        /// When the assessment is due
       
        public DateTime DueDate { get; set; }

       
        /// Current progress percentage
       
        public int ProgressPercentage { get; set; }

       
        /// Current status of the assessment
       
        public string Status { get; set; }
    }
}
