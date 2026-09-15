using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Models.Dashboard;


namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{
    /// RecentActivitySnapshot displays the last 3 user activities
    /// with Bootstrap icons, relative/absolute date formatting,
    /// and date range filtering capabilities

    public partial class RecentActivitySnapshotComponent : ComponentBase
    {
        #region Injected Dependencies

        [Inject] public ILogger<RecentActivitySnapshotComponent> Logger { get; set; }

        #endregion

        #region Properties

       
        /// Currently selected filter ID
        /// 1 = 24 hours, 2 = 7 days, 3 = 30 days, 4 = All
       
        private int SelectedFilterId = 1;

        #endregion

        #region Parameters

       
        /// List of recent activities to display
       
        [Parameter]
        public List<ActivityLogModel> Activities { get; set; }

       
        /// Callback to refresh activities when filter changes
       
        [Parameter]
        public EventCallback OnRefresh { get; set; }

        #endregion

        #region Methods

       
        /// Get filter button CSS class based on selected filter
        /// filterId: 1=24h, 2=7d, 3=30d, 4=all
       
        public string GetFilterClass(int filterId)
        {
            return SelectedFilterId == filterId ? "active" : "";
        }

       
        /// Select a filter and refresh activities
        /// filterId: 1=24h, 2=7d, 3=30d, 4=all
       
        public async Task SelectFilter(int filterId)
        {
            Logger.LogInformation("Activity filter selected: Filter ID {FilterId}", filterId);

            SelectedFilterId = filterId;

            if (OnRefresh.HasDelegate)
            {
                await OnRefresh.InvokeAsync();
            }
        }

       
        /// Format activity time to relative time string
        /// e.g., "2 minutes ago", "1 hour ago", "Yesterday"
       
        public string FormatActivityTime(DateTime createdDate)
        {
            var now = DateTime.UtcNow;
            var diff = now - createdDate;

            if (diff.TotalMinutes < 1)
                return "just now";
            else if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            else if (diff.TotalDays == 1)
                return "yesterday";
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";
            else
                return createdDate.ToString("MMM d, h:mm tt");
        }

       
        /// Get CSS class for activity action badge based on type
       
        public string GetActivityActionClass(string activityType)
        {
            return activityType switch
            {
                "View" => "action-view",
                "Edit" => "action-edit",
                "Create" => "action-create",
                "Delete" => "action-delete",
                "Approve" => "action-approve",
                "Update" => "action-update",
                "Share" => "action-share",
                _ => "action-default"
            };
        }

       
        /// Get human-readable activity action display text
       
        public string GetActivityActionDisplay(string activityType)
        {
            return activityType switch
            {
                "View" => "Viewed",
                "Edit" => "Edited",
                "Create" => "Created",
                "Delete" => "Deleted",
                "Approve" => "Approved",
                "Update" => "Updated",
                "Share" => "Shared",
                _ => activityType
            };
        }

       
        /// Get the filter type string based on selected filter ID
        /// Used when calling parent refresh
       
        public string GetSelectedFilterType()
        {
            return SelectedFilterId switch
            {
                1 => "24h",
                2 => "7d",
                3 => "30d",
                4 => "all",
                _ => "all"
            };
        }

        #endregion
    }
}
