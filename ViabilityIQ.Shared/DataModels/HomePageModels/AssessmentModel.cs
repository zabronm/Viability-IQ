using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{
    
    /// Represents an assessment in the dashboard view
    
    public class AssessmentModel
    {

        /// <summary>
        /// Unique identifier for the assessment (long/bigint)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of the assessment
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The business this assessment is for (foreign key)
        /// </summary>
        public long BusinessId { get; set; }

        /// <summary>
        /// Name of the business being assessed
        /// </summary>
        public string? BusinessName { get; set; }

        /// <summary>
        /// Current status of the assessment
        /// (InProgress, Completed, Pending, Draft, Archived)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Percentage of completion (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// When the assessment was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// When the assessment is due
        /// </summary>
        public DateTime? DueDate { get; set; }= DateTime.Now;

        /// <summary>
        /// Optional link to view the assessment
        /// </summary>
        public string? ViewUrl { get; set; }

        /// <summary>
        /// Optional link to edit the assessment
        /// </summary>
        public string? EditUrl { get; set; }
    }
}
