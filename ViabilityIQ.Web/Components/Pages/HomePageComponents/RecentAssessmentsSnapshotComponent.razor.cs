using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents;

public partial class RecentAssessmentsSnapshotComponent : ComponentBase
{
    private const int MaximumAssessmentCount = 10;

    [Inject] private IGenericDataRepository<AssessmentDto> AssessmentRepository { get; set; } = default!;
    [Inject] private ISessionService? SessionService { get; set; }
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ILogger<RecentAssessmentsSnapshotComponent> Logger { get; set; } = default!;

    // Kept temporarily so the existing HomePage markup remains source-compatible.
    // This component now loads its own live AssessmentDto records.
    [Parameter] public List<AssessmentModel>? Assessments { get; set; }

    private List<AssessmentDto> RecentAssessments { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadRecentAssessmentsAsync();
    }

    private async Task LoadRecentAssessmentsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await AssessmentRepository.GetAllAsync();
            var query = result ?? Enumerable.Empty<AssessmentDto>();

            // Future access-history filtering:
            // query = query.Where(assessment =>
            //     recentlyAccessedAssessmentIds.Contains(assessment.AssessmentId));
            // query = query.Where(assessment => assessment.Active);
            //
            // Do not filter yet. AssessmentDto does not expose a reliable
            // "last accessed by user" field, so this currently returns any 10
            // assessments ordered by creation date as requested.

            RecentAssessments = query
                .OrderByDescending(assessment => assessment.CreatedDate)
                .Take(MaximumAssessmentCount)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not load recent assessments");
            ErrorMessage = "Recent assessments could not be loaded.";
            RecentAssessments = new();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenAssessmentAsync(long assessmentId)
    {
        var selectedRecord = RecentAssessments.FirstOrDefault(
            assessment => assessment.AssessmentId == assessmentId);

        if (selectedRecord == null)
        {
            Toast.ShowError("The selected assessment is no longer available.", "Assessment");
            return;
        }

        try
        {
            SessionService?.SetActiveAssessment(
                caseNumber: selectedRecord.CaseNumber ?? string.Empty,
                assessmentId: selectedRecord.AssessmentId,
                businessId: selectedRecord.BusinessId,
                businessName: selectedRecord.BusinessName ?? string.Empty,
                clientId: selectedRecord.ClientId,
                clientName: selectedRecord.BusinessOwner ?? string.Empty,
                assessmentType: selectedRecord.AssessmentTypeName ?? string.Empty,
                HasAssetsData: true,
                HasExpensesData: true,
                HasSalesData: true,
                HasStockData: selectedRecord.blStock,
                HasReportsData: false,
                HasReviewsData: false,
                HasReviews: false,
                HasDebtorsCreditorsData: selectedRecord.blDebtorsCreditors,
                HasLoansData: true);

            Navigation.NavigateTo($"/assessment/dashboards/{selectedRecord.AssessmentId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Could not open assessment {AssessmentId}",
                selectedRecord.AssessmentId);
            Toast.ShowError($"Workspace redirection failed: {ex.Message}", "Routing Error");
        }

        await Task.CompletedTask;
    }

    private Task HandleRowKeyDownAsync(KeyboardEventArgs args, long assessmentId) =>
        args.Key is "Enter" or " "
            ? OpenAssessmentAsync(assessmentId)
            : Task.CompletedTask;

    private static int ClampProgress(long progress) =>
        (int)Math.Clamp(progress, 0, 100);

    private static string DisplayText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string GetStatusText(long statusId) => statusId switch
    {
        1 => "Draft/Setup",
        2 => "In Progress",
        3 => "Under Review",
        4 => "Approved",
        5 => "Closed/Archived",
        _ => "Unknown"
    };

    private static string GetStatusBadgeClass(long statusId) => statusId switch
    {
        1 => "status-draft",
        2 => "status-in-progress",
        3 => "status-review",
        4 => "status-approved",
        5 => "status-archived",
        _ => "status-default"
    };

    private static string GetProgressClass(long progress) => progress switch
    {
        >= 80 => "progress-high",
        >= 40 => "progress-medium",
        _ => "progress-low"
    };

}
