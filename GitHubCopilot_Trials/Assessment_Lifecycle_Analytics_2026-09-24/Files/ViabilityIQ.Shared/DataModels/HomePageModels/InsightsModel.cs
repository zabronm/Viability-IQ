using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{
    
    /// Represents analytics and insights data for the dashboard
    
    public class InsightsModel
    {
        #region Completion Rate Metrics

        
        /// Current month completion rate percentage (0-100)
        
        public int CompletionRatePercent { get; set; }

        
        /// Month-over-month trend (+/- percentage points)
        
        public int CompletionRateTrend { get; set; }

        #endregion

        #region Completion Time Metrics

        
        /// Average number of days to complete an assessment (this month)
        
        public int AverageCompletionDays { get; set; }

        
        /// Average completion days from previous month
        
        public int PreviousCompletionDays { get; set; }

        
        /// Trend in completion time (positive = slower, negative = faster)
        
        public int CompletionTimeTrend { get; set; }

        #endregion

        #region Assessment Distribution

        
        /// Count of active assessments
        
        public int ActiveCount { get; set; }

        
        /// Active assessments as percentage
        
        public int ActivePercentage { get; set; }

        
        /// Count of completed assessments
        
        public int CompletedCount { get; set; }

        
        /// Completed assessments as percentage
        
        public int CompletedPercentage { get; set; }

        
        /// Count of pending assessments
        
        public int PendingCount { get; set; }

        
        /// Pending assessments as percentage
        
        public int PendingPercentage { get; set; }

        
        /// Count of other statuses (draft, archived)
        
        public int OtherCount { get; set; }

        
        /// Other statuses as percentage
        
        public int OtherPercentage { get; set; }

        #endregion

        #region Top Performers

        
        /// List of top performers this month
        
        public List<TopPerformerModel> TopPerformers { get; set; } = new();

        #endregion
    }
}


