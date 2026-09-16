using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;

public partial class CashflowMainPagesComponent : ComponentBase, IDisposable
{
    [Inject] private ICashflowProjectionService ProjectionService { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private ILogger<CashflowMainPagesComponent> Logger { get; set; } = default!;
    [Parameter] public long AssessmentId { get; set; }

    private CashflowProjectionResult? Projection { get; set; }
    private bool IsLoading { get; set; }
    private string? ErrorMessage { get; set; }
    private long _loadedAssessmentId;
    private bool _subscribed;

    protected override void OnInitialized()
    {
        ProjectionStateManager.ProjectionChanged += OnProjectionChanged;
        _subscribed = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId != AssessmentId || Projection is null)
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Projection = await ProjectionService.CalculateAsync(AssessmentId);
            _loadedAssessmentId = AssessmentId;
        }
        catch (Exception ex)
        {
            Projection = null;
            ErrorMessage = ex.Message;
            Logger.LogError(ex, "Unable to display cashflow for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs args)
    {
        if (args.AssessmentId != AssessmentId) return;
        _ = InvokeAsync(async () =>
        {
            await LoadAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (_subscribed)
            ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;
    }
}
