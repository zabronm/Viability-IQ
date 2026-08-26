using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Models.Dashboard;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{
    
    /// RecentAssessmentsSnapshot displays the latest 5 assessments
    /// with status badges, progress bars, and action buttons
    
    public partial class RecentAssessmentsSnapshotComponent : ComponentBase
    {
        #region Parameters        
        /// List of recent assessments to display (top 5)        
        [Parameter]  public List<AssessmentModel> Assessments { get; set; }

        #endregion

        #region Methods        
        /// Get CSS class for status badge based on assessment status
        
        public string GetStatusClass(string status)
        {
            return status switch
            {
                "InProgress" => "status-in-progress",
                "Completed" => "status-completed",
                "Pending" => "status-pending",
                "Draft" => "status-draft",
                "Archived" => "status-archived",
                _ => "status-default"
            };
        }

        
        /// Get human-readable status display text
        
        public string GetStatusDisplay(string status)
        {
            return status switch
            {
                "InProgress" => "In Progress",
                "Completed" => "Completed",
                "Pending" => "Pending",
                "Draft" => "Draft",
                "Archived" => "Archived",
                _ => status
            };
        }

        
        /// Format modified date using relative format for recent dates,
        /// absolute format for dates older than 1 week
        
        public string FormatModifiedDate(DateTime date)
        {
            var now = DateTime.UtcNow;
            var diff = now - date;

            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} mins ago";
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";
            else
                return date.ToString("MMM d, h:mm tt");
        }

        
        /// Handle assessment row click (navigate to assessment)
        
        public void HandleRowClick(AssessmentModel assessment)
        {
            // TODO: Navigate to assessment detail page
        }

        
        /// Handle view button click
        
        public void HandleView(AssessmentModel assessment)
        {
            // TODO: Navigate to assessment view page
        }

        
        /// Handle edit button click
        
        public void HandleEdit(AssessmentModel assessment)
        {
            // TODO: Navigate to assessment edit page
        }

        
        /// Handle download button click
        
        public async Task HandleDownload(AssessmentModel assessment)
        {
            // TODO: Download assessment as PDF or Excel
        }

        #endregion
    }
}