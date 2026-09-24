using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Models.Dashboard;

namespace ViabilityIQ.Web.Components.Pages;

public partial class AssessmentAnalyticsPage
{
    [Inject] private IDashboardDataService DashboardDataService { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<AssessmentAnalyticsPage> Logger { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private KPIMetricsModel Metrics { get; set; } = new();
    private InsightsModel Insights { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (SessionService.UserId <= 0)
            {
                ErrorMessage = "Your authenticated user context is not available.";
                return;
            }

            var metricsTask = DashboardDataService.GetKPIMetricsAsync(SessionService.UserId);
            var insightsTask = DashboardDataService.GetInsightsAsync(SessionService.UserId);
            await Task.WhenAll(metricsTask, insightsTask);
            Metrics = await metricsTask;
            Insights = await insightsTask;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to load assessment analytics.");
            ErrorMessage = "Assessment analytics could not be loaded.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenAssessments(string _) => Navigation.NavigateTo("/settings/assessments");
}
