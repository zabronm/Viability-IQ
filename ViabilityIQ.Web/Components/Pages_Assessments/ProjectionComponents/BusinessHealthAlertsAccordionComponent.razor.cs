using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;


namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents
{
    public partial class BusinessHealthAlertsAccordionComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies
        [Inject] private IBusinessHealthAlertService? AlertService { get; set; }
        [Inject] private IProjectionStateManager? ProjectionStateManager { get; set; }
        [Inject] private ILogger<BusinessHealthAlertsAccordionComponent>? Logger { get; set; }
        [Inject] private IJSRuntime? JS { get; set; }
        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public int MaxAlerts { get; set; } = 3;

        #endregion

        #region Private Fields

        private List<BusinessHealthAlert> Alerts = new();
        private List<BusinessHealthAlert> AllAlerts = new();
        private bool IsLoading = true;
        private HashSet<string> DismissedAlertIds = new();

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation(
                    "BusinessHealthAlertsAccordionComponent initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadAlerts();

                // Subscribe to projection changes
                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug(
                        "BusinessHealthAlertsAccordionComponent subscribed to ProjectionChanged events for assessment {AssessmentId}",
                        AssessmentId);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing BusinessHealthAlertsAccordionComponent for assessment {AssessmentId}", AssessmentId);
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadAlerts()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug(
                    "Loading business health alerts for assessment {AssessmentId}",
                    AssessmentId);

                AllAlerts = await AlertService!.GetLatestAlertsAsync(AssessmentId, MaxAlerts);

                // Filter out dismissed alerts
                Alerts = AllAlerts
                    .Where(a => !DismissedAlertIds.Contains(a.AlertId))
                    .ToList();

                Logger?.LogInformation(
                    "Loaded {AlertCount} business health alerts for assessment {AssessmentId}. " +
                    "Critical: {Critical}, Warning: {Warning}, Healthy: {Healthy}. Dismissed: {Dismissed}",
                    Alerts.Count,
                    AssessmentId,
                    Alerts.Count(a => a.Severity == AlertSeverityLevel.Critical),
                    Alerts.Count(a => a.Severity == AlertSeverityLevel.Warning),
                    Alerts.Count(a => a.Severity == AlertSeverityLevel.Healthy),
                    DismissedAlertIds.Count);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading business health alerts for assessment {AssessmentId}", AssessmentId);
                Alerts = new();
                AllAlerts = new();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            // Reload alerts when any projection data changes for this assessment
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading alerts",
                    AssessmentId);

                // Reset dismissed alerts when data changes
                DismissedAlertIds.Clear();

                InvokeAsync(async () =>
                {
                    await LoadAlerts();
                    StateHasChanged();
                });
            }
        }

        private async Task DismissAlert(string alertId, int index)
        {
            try
            {
                Logger?.LogInformation(
                    "Dismissing alert {AlertId} for assessment {AssessmentId}",
                    alertId, AssessmentId);

                // Add animation class
                var element = await JS.InvokeAsync<object>("document.getElementById", $"alertItem{index}");
                await JS.InvokeVoidAsync("eval", $"document.getElementById('alertItem{index}').classList.add('dismissing')");

                // Wait for animation to complete
                await Task.Delay(300);

                // Add to dismissed set
                DismissedAlertIds.Add(alertId);

                // Remove from displayed alerts
                Alerts = Alerts.Where(a => a.AlertId != alertId).ToList();

                StateHasChanged();

                Logger?.LogDebug(
                    "Alert {AlertId} dismissed. Remaining alerts: {Count}",
                    alertId, Alerts.Count);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error dismissing alert {AlertId}", alertId);
            }
        }

        private async Task ResetAllAlerts()
        {
            try
            {
                Logger?.LogInformation(
                    "Resetting all dismissed alerts for assessment {AssessmentId}",
                    AssessmentId);

                DismissedAlertIds.Clear();
                await LoadAlerts();

                StateHasChanged();

                Logger?.LogDebug("All alerts reset for assessment {AssessmentId}", AssessmentId);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error resetting dismissed alerts");
            }
        }

        private string GetAlertHeaderColor(AlertSeverityLevel severity)
        {
            // Softer, more muted colors (70-80% of original vividity)
            return severity switch
            {
                AlertSeverityLevel.Critical => "#f87171",      // Softer red (#dc2626 → #f87171)
                AlertSeverityLevel.Warning => "#fbbf24",       // Softer orange (#f59e0b → #fbbf24)
                AlertSeverityLevel.Healthy => "#4ade80",       // Softer green (#16a34a → #4ade80)
                _ => "#60a5fa"                                  // Softer blue (#0284c7 → #60a5fa)
            };
        }

        private string GetAlertColor(AlertSeverityLevel severity)
        {
            return severity switch
            {
                AlertSeverityLevel.Critical => "#dc2626",
                AlertSeverityLevel.Warning => "#f59e0b",
                AlertSeverityLevel.Healthy => "#16a34a",
                _ => "#0284c7"
            };
        }

        private string GetAlertIcon(AlertSeverityLevel severity)
        {
            return severity switch
            {
                AlertSeverityLevel.Critical => "🔴",
                AlertSeverityLevel.Warning => "⚠️",
                AlertSeverityLevel.Healthy => "✅",
                _ => "ℹ️"
            };
        }

        private string FormatMetricValue(BusinessHealthAlert alert)
        {
            return alert.MetricLabel switch
            {
                "Operating Margin" => $"{alert.MetricValue:F1}%",
                "Expense Ratio" => $"{alert.MetricValue:F1}%",
                "Negative Months" => $"{(int)alert.MetricValue} months",
                "Minimum Balance" => $"{alert.MetricValue:C2}",
                "Sustainability Status" => alert.MetricValue == 0 ? "At Risk" : "Viable",
                _ => alert.MetricValue.ToString("F2")
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
                    Logger?.LogDebug("BusinessHealthAlertsAccordionComponent unsubscribed from ProjectionChanged events for assessment {AssessmentId}", AssessmentId);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing BusinessHealthAlertsAccordionComponent");
            }

            await Task.CompletedTask;
        }

        #endregion


        #region Detailed Guidelines for Alert Severity Levels
        private List<GuidanceItem> ParseGuidanceItems(List<string> guidanceLines)
        {
            var items = new List<GuidanceItem>();
            GuidanceItem? currentItem = null;

            foreach (var line in guidanceLines)
            {
                // Check if this is a title line (STEP, 1., 2., etc.)
                if (line.StartsWith("STEP") ||
                    line.StartsWith("1.") || line.StartsWith("2.") ||
                    line.StartsWith("3.") || line.StartsWith("4.") ||
                    line.StartsWith("5.") || line.StartsWith("6."))
                {
                    // Save previous item if exists
                    if (currentItem != null && currentItem.Lines.Any())
                    {
                        items.Add(currentItem);
                    }

                    // Start new item
                    currentItem = new GuidanceItem
                    {
                        Title = line.Trim(),
                        Lines = new List<string>()
                    };
                }
                else if (currentItem != null && !string.IsNullOrWhiteSpace(line))
                {
                    // Add line to current item
                    currentItem.Lines.Add(line);
                }
            }

            // Add final item if exists
            if (currentItem != null && currentItem.Lines.Any())
            {
                items.Add(currentItem);
            }

            return items;
        }

        // Helper class for parsed guidance items
        private class GuidanceItem
        {
            public string Title { get; set; } = string.Empty;
            public List<string> Lines { get; set; } = new();
        }
        #endregion
    }
}