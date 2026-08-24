using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{

    
    /// Represents an assessment that is due urgently (within 24 hours)
    
    public class UrgentAssessmentModel
    {

        /// <summary>
        /// Unique identifier for the alert
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The assessment ID (long/bigint)
        /// </summary>
        public long AssessmentId { get; set; }

        /// <summary>
        /// Name of the assessment
        /// </summary>
        public string AssessmentName { get; set; }

        /// <summary>
        /// The business this assessment is for
        /// </summary>
        public long BusinessId { get; set; }

        /// <summary>
        /// Name of the business being assessed
        /// </summary>
        public string BusinessName { get; set; }

        /// <summary>
        /// When the assessment is due
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Current progress percentage
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Current status of the assessment
        /// </summary>
        public string Status { get; set; }
    }
}
