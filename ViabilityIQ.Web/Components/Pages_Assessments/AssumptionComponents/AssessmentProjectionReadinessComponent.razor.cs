using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.AssumptionComponents;

public partial class AssessmentProjectionReadinessComponent
{
    [Inject] private IGenericDataRepository<Assessment> AssessmentRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentProjectionAssumptions> AssumptionsRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentSalesCategory> CategoryRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentSales> SalesRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentExpenses> ExpenseRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentLoan> LoanRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentAsset> AssetRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<DebtorsCreditorsProfile> ProfileRepository { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentProjectionReadinessComponent> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool ShowDetails { get; set; }
    private string? ErrorMessage { get; set; }
    private IReadOnlyList<ReadinessCheck> Checks { get; set; } = [];
    private long _loadedAssessmentId;

    private int ReadinessScore =>
        Checks.Count == 0 ? 0 : (int)Math.Round(Checks.Count(check => check.Passed) * 100m / Checks.Count);

    private bool HasRequiredFailure => Checks.Any(check => check.Required && !check.Passed);
    private string ReadinessStatus => HasRequiredFailure ? "Incomplete" : ReadinessScore < 90 ? "Ready with warnings" : "Ready";
    private string StatusClass => HasRequiredFailure ? "incomplete" : ReadinessScore < 90 ? "warning" : "ready";

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
            var assessmentTask = AssessmentRepository.GetByIdAsync(AssessmentId);
            var assumptionsTask = AssumptionsRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);
            var categoriesTask = CategoryRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);
            var salesTask = SalesRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);
            var expensesTask = ExpenseRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);
            var loansTask = LoanRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);
            var assetsTask = AssetRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);
            var profilesTask = ProfileRepository.GetAllAsync(item => item.AssessmentId == AssessmentId && item.Active);

            await Task.WhenAll(
                assessmentTask,
                assumptionsTask,
                categoriesTask,
                salesTask,
                expensesTask,
                loansTask,
                assetsTask,
                profilesTask);

            var assessment = await assessmentTask;
            var assumptions = (await assumptionsTask).OrderByDescending(item => item.AssessmentProjectionAssumptionsId).FirstOrDefault();
            var categories = (await categoriesTask).ToArray();
            var sales = (await salesTask).ToArray();
            var expenses = (await expensesTask).ToArray();
            var loans = (await loansTask).ToArray();
            var assets = (await assetsTask).ToArray();
            var profiles = (await profilesTask).ToArray();

            var hasOpeningPosition =
                assessment is not null
                && (assessment.OpeningBalance_Bank != 0m
                    || assessment.OpeningBalance_Assets != 0m
                    || categories.Any(category =>
                        category.OpeningStock != 0m
                        || category.OpeningDebtorsAmount != 0m
                        || category.OpeningCreditorsAmount != 0m));

            var hasSalesValues = sales.Any(HasMonthlyValue);
            var profile = profiles.FirstOrDefault();
            var profileValid = profile is not null
                && DebtorProfileTotal(profile) <= 100.01m
                && CreditorProfileTotal(profile) <= 100.01m;
            var loansComplete = loans.All(loan =>
                loan.LoanBalanceAtAssessmentDate >= 0m
                && loan.InterestRatePerAnnum >= 0m
                && loan.RepaymentPeriodMonths > 0
                && loan.StartMonth is >= 1 and <= 12);
            var assetsComplete = assets.All(asset =>
                !asset.IsDepreciable
                || (asset.DepreciationRate > 0m && !string.IsNullOrWhiteSpace(asset.DepreciationMethod)));

            Checks =
            [
                new("Opening position", hasOpeningPosition,
                    "Opening bank, assets or category working-capital balances are available.",
                    "Confirm opening bank, stock, debtors, creditors, assets and loans—even where valid balances are zero.", true),
                new("Sales Categories", categories.Length > 0,
                    $"{categories.Length} active Sales Category record(s) found.",
                    "Create at least one Sales Category and assign its income type.", true),
                new("Monthly Sales", hasSalesValues,
                    $"{sales.Length} active Sales line(s) reviewed.",
                    "Enter at least one non-zero monthly Sales or income value.", true),
                new("Debtor and creditor profile", profileValid,
                    profileValid ? "Collection and payment profiles are available." : "The profile is missing or its explicit ageing buckets exceed 100%.",
                    "Complete the debtor and creditor profile; the implicit current bucket is the balance to 100%.", true),
                new("Operating expenses", expenses.Length > 0,
                    $"{expenses.Length} active expense line(s) found.",
                    "Enter the normal operating expenses required to run the business.", true),
                new("VAT status", assessment?.blVat > 0 || assessment?.VATRate >= 0,
                    $"VAT configuration is recorded at {assessment?.VATRate ?? 0}%.",
                    "Confirm whether the business is VAT registered and verify the default rate.", true),
                new("Loan details", loansComplete,
                    loans.Length == 0 ? "No active loans require completion." : $"{loans.Length} active loan(s) have usable terms.",
                    "Complete each loan balance, interest rate, repayment term and start month.", false),
                new("Asset depreciation", assetsComplete,
                    assets.Length == 0 ? "No active assets require depreciation settings." : $"{assets.Length} active asset(s) reviewed.",
                    "Add a positive rate and method for every depreciable asset.", false),
                new("Core assumptions", assumptions is not null,
                    assumptions is null ? "No lightweight baseline assumption record exists." : "A baseline assumption record is available.",
                    "Save the Sales, expense and funding assumptions.", true),
                new("Baseline confirmation", assumptions?.IsConfirmed == true,
                    assumptions?.IsConfirmed == true ? "The baseline assumptions have been confirmed." : "The assumptions are still marked as draft.",
                    "Review the assumptions and tick the confirmation statement before projections are approved.", false)
            ];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Logger.LogError(ex, "Unable to evaluate readiness for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool HasMonthlyValue(AssessmentSales sale) =>
        sale.Month_1 != 0m || sale.Month_2 != 0m || sale.Month_3 != 0m || sale.Month_4 != 0m
        || sale.Month_5 != 0m || sale.Month_6 != 0m || sale.Month_7 != 0m || sale.Month_8 != 0m
        || sale.Month_9 != 0m || sale.Month_10 != 0m || sale.Month_11 != 0m || sale.Month_12 != 0m;

    private static decimal DebtorProfileTotal(DebtorsCreditorsProfile profile) =>
        profile.Debtors_30 + profile.Debtors_60 + profile.Debtors_90 + profile.Debtors_120 + profile.Debtors_120Plus;

    private static decimal CreditorProfileTotal(DebtorsCreditorsProfile profile) =>
        profile.Creditors_30 + profile.Creditors_60 + profile.Creditors_90 + profile.Creditors_120 + profile.Creditors_120Plus;

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

    private sealed record ReadinessCheck(
        string Title,
        bool Passed,
        string Message,
        string Action,
        bool Required);
}
