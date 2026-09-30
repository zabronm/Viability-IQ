using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAccs_Page
{
    [Parameter]
    public long AssessmentId { get; set; }

    [Inject]
    private IAccountsProjectionService ProjectionService { get; set; } = default!;

    [Inject]
    private ILogger<AssessmentAccs_Page> Logger { get; set; } = default!;

    private AccountsProjectionResult? Projection { get; set; }
    private AccountsTab ActiveTab { get; set; } = AccountsTab.Debtors;
    private bool IsLoading { get; set; }
    private string? ErrorMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (AssessmentId <= 0)
        {
            ErrorMessage = "A valid assessment ID is required.";
            Projection = null;
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Projection = await ProjectionService.CalculateAsync(AssessmentId);
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to load accounts projection for assessment {AssessmentId}",
                AssessmentId);
            ErrorMessage = "The live assessment data could not be calculated. Review the application log for details.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectTab(AccountsTab tab) => ActiveTab = tab;

    private enum AccountsTab
    {
        Debtors,
        Creditors,
        Consolidated
    }
}
