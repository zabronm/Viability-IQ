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

       
        /// Unique identifier for the assessment (long/bigint)
       
        public long Id { get; set; }

       
        /// Name of the assessment
       
        public string? Name { get; set; }

       
        /// The business this assessment is for (foreign key)
       
        public long BusinessId { get; set; }

       
        /// Name of the business being assessed
       
        public string? BusinessName { get; set; }

       
        /// Current status of the assessment
        /// (InProgress, Completed, Pending, Draft, Archived)
       
        public string? Status { get; set; }

       
        /// Percentage of completion (0-100)
       
        public int ProgressPercentage { get; set; }

       
        /// When the assessment was last modified
       
        public DateTime ModifiedDate { get; set; }

       
        /// When the assessment is due
       
        public DateTime? DueDate { get; set; }= DateTime.Now;

       
        /// Optional link to view the assessment
       
        public string? ViewUrl { get; set; }

       
        /// Optional link to edit the assessment
       
        public string? EditUrl { get; set; }
    }
}
