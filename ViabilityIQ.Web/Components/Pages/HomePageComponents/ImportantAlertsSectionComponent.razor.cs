using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Models.Dashboard;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{

    /// ImportantAlertsSection displays urgent assessments, upcoming deadlines,
    /// and system announcements with dismissal capability

    public partial class ImportantAlertsSectionComponent : ComponentBase
    {
        #region Injected Dependencies

        [Inject] public ILogger<ImportantAlertsSectionComponent> Logger { get; set; }

        #endregion

        #region Parameters

        /// <summary>
        /// Alert data containing urgent and upcoming assessments
        /// </summary>
        [Parameter]
        public AlertsModel Alerts { get; set; }

        /// <summary>
        /// System announcements to display
        /// </summary>
        [Parameter]
        public List<SystemAnnouncementModel> Announcements { get; set; }

        /// <summary>
        /// Callback when user dismisses an alert
        /// Parameters: (alertId, actionTypeId)
        /// actionTypeId: 1=UrgentAssessment, 2=Announcement
        /// </summary>
        [Parameter]
        public EventCallback<(string alertId, int actionTypeId)> OnDismiss { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Format countdown text for due date
        /// Shows time remaining until due date
        /// </summary>
        public string FormatCountdown(DateTime dueDate)
        {
            var now = DateTime.UtcNow;
            var diff = dueDate - now;

            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} minutes left";
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hours left";
            else
                return $"{(int)diff.TotalDays} day{((int)diff.TotalDays > 1 ? "s" : "")} left";
        }

        /// <summary>
        /// Handle marking an assessment as done
        /// actionTypeId: 1=Mark urgent assessment as done
        /// </summary>
        public async Task HandleMarkDone(string alertId, int actionTypeId)
        {
            Logger.LogInformation("Mark done clicked: Alert ID {AlertId}, Action Type ID {ActionTypeId}",
                alertId, actionTypeId);

            switch (actionTypeId)
            {
                case 1: // Mark urgent assessment as done
                    Logger.LogInformation("Marking urgent assessment as done: {AlertId}", alertId);
                    // TODO: Call service to mark assessment as completed
                    break;
                default:
                    Logger.LogWarning("Unknown action type ID: {ActionTypeId}", actionTypeId);
                    break;
            }
        }

        /// <summary>
        /// Handle dismissing an alert or announcement
        /// Stores dismissal in database and removes from view
        /// actionTypeId: 1=UrgentAssessment, 2=Announcement
        /// </summary>
        public async Task HandleDismissAlert(string alertId, int actionTypeId)
        {
            Logger.LogInformation("Dismiss alert clicked: Alert ID {AlertId}, Action Type ID {ActionTypeId}",
                alertId, actionTypeId);

            if (OnDismiss.HasDelegate)
            {
                await OnDismiss.InvokeAsync((alertId, actionTypeId));
            }
        }

        #endregion
    }
}

