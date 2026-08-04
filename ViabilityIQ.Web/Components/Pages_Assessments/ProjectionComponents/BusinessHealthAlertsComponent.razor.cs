using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents
{

    /// Component that displays business health alerts and guidance

    public partial class BusinessHealthAlertsComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies
        [Inject] private IBusinessHealthAlertService AlertService { get; set; } = default!;
        [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
        [Inject] private ILogger<BusinessHealthAlertsComponent> Logger { get; set; } = default!;

        #endregion

        #region Parameters


        /// The assessment ID to display alerts for        
        [Parameter] public long AssessmentId { get; set; }

        /// Maximum number of alerts to display (default 3)        
        [Parameter] public int MaxAlerts { get; set; } = 3;
        #endregion

        #region Private Fields
        private List<BusinessHealthAlert> Alerts = new();
        private bool IsLoading = true;
        private HashSet<string> DismissedAlertIds = new();
        #endregion

        #region Lifecycle Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger.LogInformation("BusinessHealthAlertsComponent initialized for assessment {AssessmentId}", AssessmentId);

                await LoadAlerts();

                // Subscribe to projection changes
                ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                Logger.LogDebug("BusinessHealthAlertsComponent subscribed to ProjectionChanged events for assessment {AssessmentId}", AssessmentId);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error initializing BusinessHealthAlertsComponent for assessment {AssessmentId}", AssessmentId); IsLoading = false;
            }
        }

        #endregion

        #region Private Methods


        /// Loads and displays the latest alerts for the assessment

        private async Task LoadAlerts()
        {
            try
            {
                IsLoading = true;

                Logger.LogDebug("Loading business health alerts for assessment {AssessmentId}", AssessmentId);

                var allAlerts = await AlertService.GetLatestAlertsAsync(AssessmentId, MaxAlerts);


                // Filter out dismissed alerts
                Alerts = allAlerts
                    .Where(a => !DismissedAlertIds.Contains(a.AlertId))
                    .ToList();

                Logger.LogInformation(
                    "Loaded {AlertCount} business health alerts for assessment {AssessmentId}. " +
                    "Critical: {Critical}, Warning: {Warning}, Healthy: {Healthy}",
                    Alerts.Count,
                    AssessmentId,
                    Alerts.Count(a => a.Severity == AlertSeverityLevel.Critical),
                    Alerts.Count(a => a.Severity == AlertSeverityLevel.Warning),
                    Alerts.Count(a => a.Severity == AlertSeverityLevel.Healthy));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading business health alerts for assessment {AssessmentId}",
                    AssessmentId); Alerts = new();
            }
            finally
            {
                IsLoading = false;
            }
        }


        /// Called when projections change - refreshes alerts

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            // Reload alerts when any projection data changes for this assessment
            if (e.AssessmentId == AssessmentId)
            {
                Logger.LogInformation("Projection changed event received for assessment {AssessmentId}, reloading alerts", AssessmentId);

                InvokeAsync(async () =>
                {
                    await LoadAlerts();
                });
            }
        }


        /// Converts BusinessHealthAlert severity to ViqAlertComponent severity

        private ViqAlertComponent.AlertSeverity ConvertSeverity(AlertSeverityLevel severity)
        {
            return severity switch
            {
                AlertSeverityLevel.Critical => ViqAlertComponent.AlertSeverity.Danger,
                AlertSeverityLevel.Warning => ViqAlertComponent.AlertSeverity.Warning,
                AlertSeverityLevel.Healthy => ViqAlertComponent.AlertSeverity.Success,
                _ => ViqAlertComponent.AlertSeverity.Info
            };
        }


        /// Builds the alert message combining message and recommendation

        private string BuildAlertMessage(BusinessHealthAlert alert)
        {
            var parts = new List<string> { alert.Message };

            if (!string.IsNullOrEmpty(alert.Recommendation))
            {
                parts.Add($"💡 {alert.Recommendation}");
            }

            if (!string.IsNullOrEmpty(alert.MetricLabel) && alert.MetricValue != 0)
            {
                parts.Add($"📊 {alert.MetricLabel}: {FormatMetricValue(alert)}");
            }

            return string.Join(" | ", parts);
        }


        /// Formats metric value based on type        
        private string FormatMetricValue(BusinessHealthAlert alert)
        {
            return alert.MetricLabel switch
            {
                "Operating Margin" => $"{alert.MetricValue:F1}%",
                "Expense Ratio" => $"{alert.MetricValue:F1}%",
                "Negative Months" => $"{(int)alert.MetricValue} months",
                "Minimum Balance" => $"{alert.MetricValue:C2}",
                _ => alert.MetricValue.ToString("F2")
            };
        }


        /// Gets color for guidance section based on alert severity

        private string GetGuidanceColor(AlertSeverityLevel severity)
        {
            return severity switch
            {
                AlertSeverityLevel.Critical => "#dc3545",
                AlertSeverityLevel.Warning => "#ffc107",
                AlertSeverityLevel.Healthy => "#28a745",
                _ => "#17a2b8"
            };
        }
        #endregion

        #region Disposal
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            try
            {
                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;
                    Logger.LogDebug("BusinessHealthAlertsComponent unsubscribed from ProjectionChanged events for assessment {AssessmentId}", AssessmentId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing BusinessHealthAlertsComponent");
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}