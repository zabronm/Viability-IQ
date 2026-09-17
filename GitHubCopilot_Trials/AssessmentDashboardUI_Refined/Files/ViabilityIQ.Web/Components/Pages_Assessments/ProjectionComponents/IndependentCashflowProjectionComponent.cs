using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;

public abstract class IndependentCashflowProjectionComponent : ComponentBase, IDisposable
{
    [Inject] private ICashflowProjectionService ProjectionService { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }
    [Parameter] public CashflowProjectionResult? Projection { get; set; }

    protected bool IsLoading { get; private set; } = true;
    protected string? LoadError { get; private set; }

    private long _loadedAssessmentId;
    private bool _subscribed;
    private bool _usesSuppliedProjection;

    protected override void OnInitialized()
    {
        ProjectionStateManager.ProjectionChanged += OnProjectionChanged;
        _subscribed = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId == 0 && Projection is not null)
            _usesSuppliedProjection = true;

        if (_usesSuppliedProjection)
        {
            IsLoading = false;
            LoadError = null;
            await OnProjectionLoadedAsync();
            return;
        }

        if (_loadedAssessmentId != AssessmentId || Projection is null)
            await LoadProjectionAsync();
    }

    protected virtual Task OnProjectionLoadedAsync() => Task.CompletedTask;

    private async Task LoadProjectionAsync()
    {
        IsLoading = true;
        LoadError = null;

        try
        {
            if (AssessmentId <= 0)
                throw new InvalidOperationException("No active assessment was supplied.");

            Projection = await ProjectionService.CalculateAsync(AssessmentId);
            _loadedAssessmentId = AssessmentId;
            await OnProjectionLoadedAsync();
        }
        catch (Exception ex)
        {
            Projection = null;
            LoadError = ex.Message;
            LoggerFactory
                .CreateLogger(GetType())
                .LogError(ex, "Unable to load {ComponentName} for assessment {AssessmentId}", GetType().Name, AssessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs args)
    {
        if (_usesSuppliedProjection || args.AssessmentId != AssessmentId)
            return;

        _ = InvokeAsync(async () =>
        {
            await LoadProjectionAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (_subscribed)
            ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;

        GC.SuppressFinalize(this);
    }
}
