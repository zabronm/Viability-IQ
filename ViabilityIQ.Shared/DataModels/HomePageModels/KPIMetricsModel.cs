namespace ViabilityIQ.Web.Models.Dashboard
{

    
    /// Represents the 6 KPI metrics displayed on the dashboard
    
    public class KPIMetricsModel
    {
        #region Personal Metrics (Row 1)

        
        /// Number of active assessments assigned to the user
        
        public int ActiveAssessments { get; set; }

        
        /// Month-over-month change in active assessments
        
        public int ActiveAssessmentsChange { get; set; }

        
        /// Number of assessments completed by the user this month
        
        public int CompletedAssessments { get; set; }

        
        /// Month-over-month change in completed assessments
        
        public int CompletedAssessmentsChange { get; set; }

        
        /// Number of assessments awaiting user's review/approval
        
        public int PendingReviews { get; set; }

        
        /// Day-over-day change in pending reviews
        
        public int PendingReviewsChange { get; set; }

        #endregion

        #region Organizational Metrics (Row 2)

        
        /// Total assessments assigned to the user (all statuses)
        
        public int YourWorkload { get; set; }

        
        /// Month-over-month change in user's workload
        
        public int YourWorkloadChange { get; set; }

        
        /// Total unique clients in the system or user's branch
        
        public int TotalClientBase { get; set; }

        
        /// Year-over-year change in client base
        
        public int TotalClientBaseChange { get; set; }

        
        /// Total assessments in the user's branch (all statuses)
        
        public int BranchAssessments { get; set; }

        
        /// Month-over-month change in branch assessments
        
        public int BranchAssessmentsChange { get; set; }

        #endregion
    }
}
