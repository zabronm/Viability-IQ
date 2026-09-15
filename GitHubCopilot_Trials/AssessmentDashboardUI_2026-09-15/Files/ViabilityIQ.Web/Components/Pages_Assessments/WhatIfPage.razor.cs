using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class WhatIfPage : ComponentBase, IDisposable
{
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private ICashflowProjectionService ProjectionService { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private ILogger<WhatIfPage> Logger { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }

    private CashflowProjectionResult? Projection { get; set; }
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private long _activeAssessmentId;
    private bool _subscribed;

    private CashflowProjectionAlert? PrimaryAlert =>
        Projection?.Alerts
            .OrderByDescending(alert => alert.Severity)
            .FirstOrDefault();

    private string AlertHeading => PrimaryAlert?.Title
        ?? (Projection?.Summary.ProfitBeforeTax >= 0m ? "WELL DONE:" : "ATTENTION:");

    private string AlertMessage => PrimaryAlert?.Message
        ?? (Projection?.Summary.ProfitBeforeTax >= 0m
            ? "The assessment projects a positive profit before tax for the period."
            : "The assessment projects a loss before tax. Review the monthly performance and business indicators.");

    private string AlertClass => PrimaryAlert?.Severity switch
    {
        CashflowAlertSeverity.Critical => "dashboard-alert-critical",
        CashflowAlertSeverity.Warning => "dashboard-alert-warning",
        _ when Projection?.Summary.ProfitBeforeTax < 0m => "dashboard-alert-critical",
        _ => "dashboard-alert-healthy"
    };

    private string AlertIcon => AlertClass switch
    {
        "dashboard-alert-critical" => "bi-exclamation-octagon-fill",
        "dashboard-alert-warning" => "bi-exclamation-triangle-fill",
        _ => "bi-check-circle-fill"
    };

    protected override void OnInitialized()
    {
        ProjectionStateManager.ProjectionChanged += OnProjectionChanged;
        _subscribed = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        var requestedAssessmentId = AssessmentId > 0
            ? AssessmentId
            : SessionService.AssessmentId.GetValueOrDefault();

        if (requestedAssessmentId <= 0)
        {
            Projection = null;
            ErrorMessage = "No active assessment was found.";
            IsLoading = false;
            Toast.ShowError("Case number is unknown. Please restart the application.");
            return;
        }

        if (_activeAssessmentId != requestedAssessmentId || Projection is null)
            await LoadProjectionAsync(requestedAssessmentId);
    }

    private async Task LoadProjectionAsync(long assessmentId)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Projection = await ProjectionService.CalculateAsync(assessmentId);
            _activeAssessmentId = assessmentId;
        }
        catch (Exception ex)
        {
            Projection = null;
            ErrorMessage = ex.Message;
            Logger.LogError(ex, "Unable to load dashboard projection for assessment {AssessmentId}", assessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs args)
    {
        if (args.AssessmentId != _activeAssessmentId)
            return;

        _ = InvokeAsync(async () =>
        {
            await LoadProjectionAsync(_activeAssessmentId);
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (_subscribed)
            ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;
    }
}
