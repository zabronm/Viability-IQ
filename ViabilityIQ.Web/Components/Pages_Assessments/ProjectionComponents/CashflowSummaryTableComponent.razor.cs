using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;


namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents
{
    public partial class CashflowSummaryTableComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies
        [Inject] private ICashflowEngine? CashflowEngine { get; set; }
        [Inject] private IProjectionStateManager? ProjectionStateManager { get; set; }
        [Inject] private ILogger<CashflowSummaryTableComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private CashflowSummaryDto? Summary { get; set; }
        private bool IsLoading { get; set; } = true;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation("CashflowSummaryTableComponent initialized for assessment {AssessmentId}", AssessmentId);

                await LoadCashflowSummary();

                // Subscribe to projection changes
                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug("CashflowSummaryTableComponent subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing CashflowSummaryTableComponent");
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadCashflowSummary()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug("Loading cashflow summary for assessment {AssessmentId}", AssessmentId);

                Summary = await CashflowEngine!.GetCashflowSummaryDisplayAsync(AssessmentId);

                if (Summary != null)
                {
                    Logger?.LogInformation(
                        "Loaded cashflow summary for assessment {AssessmentId}. " +
                        "Income: {Income}, Expense: {Expense}, Net: {Net}, Sustainable: {Sustainable}",
                        AssessmentId,
                        Summary.TotalAnnualIncome,
                        Summary.TotalAnnualExpense,
                        Summary.TotalAnnualNetCashflow,
                        Summary.IsSustainable);
                }
                else
                {
                    Logger?.LogWarning("Cashflow summary is null for assessment {AssessmentId}", AssessmentId);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading cashflow summary for assessment {AssessmentId}", AssessmentId);
                Summary = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            // Reload summary when any projection data changes for this assessment
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading summary", AssessmentId);

                InvokeAsync(async () =>
                {
                    await LoadCashflowSummary();
                    StateHasChanged();
                });
            }
        }

        /// <summary>
        /// Returns color based on operating margin
        /// Green: >= 15%, Orange: 5-14%, Red: < 5%
        /// </summary>
        private string GetMarginColor(decimal margin)
        {
            return margin switch
            {
                >= 15m => "#16a34a",      // Green
                >= 5m => "#f59e0b",       // Orange
                _ => "#dc2626"            // Red
            };
        }

        /// <summary>
        /// Returns color based on expense ratio
        /// Green: <= 70%, Orange: 71-85%, Red: > 85%
        /// </summary>
        private string GetExpenseRatioColor(decimal ratio)
        {
            return ratio switch
            {
                <= 70m => "#16a34a",      // Green
                <= 85m => "#f59e0b",      // Orange
                _ => "#dc2626"            // Red
            };
        }

        /// <summary>
        /// Returns color based on negative months count
        /// Green: 0, Yellow: 1-2, Orange: 3-4, Red: >= 5
        /// </summary>
        private string GetNegativeMonthsColor(int months)
        {
            return months switch
            {
                0 => "#16a34a",           // Green
                1 or 2 => "#eab308",      // Yellow
                3 or 4 => "#f59e0b",      // Orange
                _ => "#dc2626"            // Red
            };
        }

        /// <summary>
        /// Returns color based on critical months count
        /// Green: 0, Yellow: 1, Orange: 2, Red: >= 3
        /// </summary>
        private string GetCriticalMonthsColor(int months)
        {
            return months switch
            {
                0 => "#16a34a",           // Green
                1 => "#eab308",           // Yellow
                2 => "#f59e0b",           // Orange
                _ => "#dc2626"            // Red
            };
        }

        /// <summary>
        /// Returns color and text based on health status
        /// </summary>
        private string GetHealthStatusColor(CashflowHealthStatus status)
        {
            return status switch
            {
                CashflowHealthStatus.Healthy => "#16a34a",    // Green
                CashflowHealthStatus.Warning => "#f59e0b",    // Orange
                CashflowHealthStatus.Critical => "#dc2626",   // Red
                _ => "#6b7280"                                  // Gray
            };
        }

        private string GetHealthStatusText(CashflowHealthStatus status)
        {
            return status switch
            {
                CashflowHealthStatus.Healthy => "✓ Healthy",
                CashflowHealthStatus.Warning => "⚠ Warning",
                CashflowHealthStatus.Critical => "✕ Critical",
                _ => "Unknown"
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
                    Logger?.LogDebug("CashflowSummaryTableComponent unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing CashflowSummaryTableComponent");
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}