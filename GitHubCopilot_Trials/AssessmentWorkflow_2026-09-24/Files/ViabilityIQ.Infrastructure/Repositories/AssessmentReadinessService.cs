using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class AssessmentReadinessService : IAssessmentReadinessService
{
    private readonly IGenericDataRepository<Assessment> _assessmentRepository;
    private readonly IGenericDataRepository<AssessmentProjectionAssumptions> _assumptionsRepository;
    private readonly IGenericDataRepository<AssessmentSalesCategory> _categoryRepository;
    private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
    private readonly IGenericDataRepository<AssessmentExpenses> _expenseRepository;
    private readonly IGenericDataRepository<AssessmentLoan> _loanRepository;
    private readonly IGenericDataRepository<AssessmentAsset> _assetRepository;
    private readonly IGenericDataRepository<DebtorsCreditorsProfile> _profileRepository;

    public AssessmentReadinessService(
        IGenericDataRepository<Assessment> assessmentRepository,
        IGenericDataRepository<AssessmentProjectionAssumptions> assumptionsRepository,
        IGenericDataRepository<AssessmentSalesCategory> categoryRepository,
        IGenericDataRepository<AssessmentSales> salesRepository,
        IGenericDataRepository<AssessmentExpenses> expenseRepository,
        IGenericDataRepository<AssessmentLoan> loanRepository,
        IGenericDataRepository<AssessmentAsset> assetRepository,
        IGenericDataRepository<DebtorsCreditorsProfile> profileRepository)
    {
        _assessmentRepository = assessmentRepository;
        _assumptionsRepository = assumptionsRepository;
        _categoryRepository = categoryRepository;
        _salesRepository = salesRepository;
        _expenseRepository = expenseRepository;
        _loanRepository = loanRepository;
        _assetRepository = assetRepository;
        _profileRepository = profileRepository;
    }

    public async Task<AssessmentReadinessResult> EvaluateAsync(long assessmentId)
    {
        var assessmentTask = _assessmentRepository.GetByIdAsync(assessmentId);
        var assumptionsTask = _assumptionsRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var categoriesTask = _categoryRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var salesTask = _salesRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var expensesTask = _expenseRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var loansTask = _loanRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var assetsTask = _assetRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var profilesTask = _profileRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);

        await Task.WhenAll(
            assessmentTask, assumptionsTask, categoriesTask, salesTask,
            expensesTask, loansTask, assetsTask, profilesTask);

        var assessment = await assessmentTask;
        var assumptions = (await assumptionsTask)
            .OrderByDescending(item => item.AssessmentProjectionAssumptionsId)
            .FirstOrDefault();
        var categories = (await categoriesTask).ToArray();
        var sales = (await salesTask).ToArray();
        var expenses = (await expensesTask).ToArray();
        var loans = (await loansTask).ToArray();
        var assets = (await assetsTask).ToArray();
        var profiles = (await profilesTask).ToArray();

        var hasOpeningPosition = assessment is not null
            && (assessment.OpeningBalance_Bank != 0m
                || assessment.OpeningBalance_Assets != 0m
                || categories.Any(category =>
                    category.OpeningStock != 0m
                    || category.OpeningDebtorsAmount != 0m
                    || category.OpeningCreditorsAmount != 0m));
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

        AssessmentReadinessCheck[] checks =
        [
            new("Opening position", hasOpeningPosition,
                "Opening bank, assets or working-capital balances are available.",
                "Confirm opening bank, stock, debtors, creditors, assets and loans.", true),
            new("Sales categories", categories.Length > 0,
                $"{categories.Length} active sales category record(s) found.",
                "Create at least one sales category and assign its income type.", true),
            new("Monthly sales", sales.Any(HasMonthlyValue),
                $"{sales.Length} active sales line(s) reviewed.",
                "Enter at least one non-zero monthly sales or income value.", true),
            new("Debtor and creditor profile", profileValid,
                profileValid ? "Collection and payment profiles are available." : "The profile is missing or ageing buckets exceed 100%.",
                "Complete the debtor and creditor profile.", true),
            new("Operating expenses", expenses.Length > 0,
                $"{expenses.Length} active expense line(s) found.",
                "Enter the operating expenses required to run the business.", true),
            new("VAT status", assessment?.blVat > 0 || assessment?.VATRate >= 0,
                $"VAT configuration is recorded at {assessment?.VATRate ?? 0}%.",
                "Confirm VAT registration and the default rate.", true),
            new("Loan details", loansComplete,
                loans.Length == 0 ? "No active loans require completion." : $"{loans.Length} active loan(s) have usable terms.",
                "Complete each loan balance, interest rate, repayment term and start month.", false),
            new("Asset depreciation", assetsComplete,
                assets.Length == 0 ? "No active assets require depreciation settings." : $"{assets.Length} active asset(s) reviewed.",
                "Add a positive rate and method for every depreciable asset.", false),
            new("Core assumptions", assumptions is not null,
                assumptions is null ? "No baseline assumption record exists." : "A baseline assumption record is available.",
                "Save the sales, expense and funding assumptions.", true),
            new("Baseline confirmation", assumptions?.IsConfirmed == true,
                assumptions?.IsConfirmed == true ? "Baseline assumptions are confirmed." : "The assumptions are still draft.",
                "Review and confirm the assumptions before completion.", false)
        ];

        var score = (int)Math.Round(checks.Count(check => check.Passed) * 100m / checks.Length);
        return new AssessmentReadinessResult
        {
            AssessmentId = assessmentId,
            Score = score,
            CanComplete = checks.Where(check => check.Required).All(check => check.Passed),
            Checks = checks
        };
    }

    private static bool HasMonthlyValue(AssessmentSales sale) =>
        sale.Month_1 != 0m || sale.Month_2 != 0m || sale.Month_3 != 0m || sale.Month_4 != 0m
        || sale.Month_5 != 0m || sale.Month_6 != 0m || sale.Month_7 != 0m || sale.Month_8 != 0m
        || sale.Month_9 != 0m || sale.Month_10 != 0m || sale.Month_11 != 0m || sale.Month_12 != 0m;

    private static decimal DebtorProfileTotal(DebtorsCreditorsProfile profile) =>
        profile.Debtors_30 + profile.Debtors_60 + profile.Debtors_90 + profile.Debtors_120 + profile.Debtors_120Plus;

    private static decimal CreditorProfileTotal(DebtorsCreditorsProfile profile) =>
        profile.Creditors_30 + profile.Creditors_60 + profile.Creditors_90 + profile.Creditors_120 + profile.Creditors_120Plus;
}
