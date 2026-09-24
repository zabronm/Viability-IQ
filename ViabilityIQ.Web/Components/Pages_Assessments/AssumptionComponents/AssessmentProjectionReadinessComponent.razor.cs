using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.AssumptionComponents;

public partial class AssessmentProjectionReadinessComponent
{
    [Inject] private IAssessmentReadinessService ReadinessService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentProjectionReadinessComponent> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool ShowDetails { get; set; }
    private string? ErrorMessage { get; set; }
    private IReadOnlyList<AssessmentReadinessCheck> Checks { get; set; } = [];
    private long _loadedAssessmentId;

    private int ReadinessScore =>
        Checks.Count == 0 ? 0 : (int)Math.Round(Checks.Count(check => check.Passed) * 100m / Checks.Count);
    private bool HasRequiredFailure => Checks.Any(check => check.Required && !check.Passed);
    private string ReadinessStatus => HasRequiredFailure ? "Incomplete" : ReadinessScore < 100 ? "Ready with warnings" : "Ready";
    private string StatusClass => HasRequiredFailure ? "incomplete" : ReadinessScore < 100 ? "warning" : "ready";

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId == AssessmentId)
            return;

        _loadedAssessmentId = AssessmentId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await ReadinessService.EvaluateAsync(AssessmentId);
            Checks = result.Checks;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Projection readiness could not be evaluated.";
            Logger.LogError(ex, "Unable to evaluate readiness for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ToggleDetails() => ShowDetails = !ShowDetails;

    private async Task PrintAsync()
    {
        var restoreCollapsedState = !ShowDetails;
        ShowDetails = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            await JS.InvokeVoidAsync("print");
        }
        finally
        {
            if (restoreCollapsedState)
            {
                ShowDetails = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
