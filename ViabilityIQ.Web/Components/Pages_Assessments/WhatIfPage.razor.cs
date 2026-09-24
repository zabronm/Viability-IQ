using Microsoft.AspNetCore.Components;
using Serilog.Core;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Projections;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class WhatIfPage: IAsyncDisposable
{
    [Inject] private IProjectionStateManager? projectionStateManager { get; set; }
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ILogger<AssessmentSalesPage>? Logger { get; set; }

    [Parameter] public long AssessmentId { get; set; }

    private long ActiveAssessmentId { get; set; }
    private string? ErrorMessage { get; set; }

    protected override void OnParametersSet()
    {
        ActiveAssessmentId = AssessmentId > 0
            ? AssessmentId
            : SessionService.AssessmentId.GetValueOrDefault();

        // Subscribe to projection changes
        if (projectionStateManager != null)
        {
            projectionStateManager.ProjectionChanged += OnProjectionChanged;
            Logger?.LogDebug("AssessmentVATPage subscribed to ProjectionChanged events");
        }

        ErrorMessage = ActiveAssessmentId > 0
            ? null
            : "No active assessment was found.";

        if (ActiveAssessmentId <= 0)
            Toast.ShowError("Case number is unknown. Please restart the application.");
    }



    /// Handle projection state changes        
    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs e)
    {
        try
        {
            if (e != null && e.AssessmentId == AssessmentId && e.DataType == "Assumptions")
            {
                Logger?.LogInformation(
                    "Assessment data changed externally, refreshing for assessment {AssessmentId}",
                    AssessmentId);

                // Refresh VAT data
                InvokeAsync(async () =>
                {
                    // await LoadAndMapVATData();
                    StateHasChanged();
                });
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error handling projection change");
        }
    }



    // ====================================================
    // CLEANUP
    // ====================================================
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        // Unsubscribe from events
        if (projectionStateManager != null)
        {
            projectionStateManager.ProjectionChanged -= OnProjectionChanged;
        }
        await Task.CompletedTask;
    }
}
