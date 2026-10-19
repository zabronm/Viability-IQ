using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class AssessmentReadinessService : IAssessmentReadinessService
{
    private readonly IGenericDataRepository<Assessment> _assessmentRepository;
    private readonly IGenericDataRepository<AssessmentProjectionAssumptions> _assumptionsRepository;
    private readonly IGenericDataRepository<AssessmentSalesCategory> _categoryRepository;
    private readonly IGenericDataRepository<AssessmentStock> _stockRepository;
    private readonly IGenericDataRepository<AssessmentManufacturingMaterial> _manufacturingMaterialRepository;
    private readonly IGenericDataRepository<AssessmentManufacturingBalances> _manufacturingBalancesRepository;
    private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
    private readonly IGenericDataRepository<AssessmentExpenses> _expenseRepository;
    private readonly IGenericDataRepository<AssessmentLoan> _loanRepository;
    private readonly IGenericDataRepository<AssessmentAsset> _assetRepository;
    private readonly IGenericDataRepository<DebtorsCreditorsProfile> _profileRepository;

    public AssessmentReadinessService(
        IGenericDataRepository<Assessment> assessmentRepository,
        IGenericDataRepository<AssessmentProjectionAssumptions> assumptionsRepository,
        IGenericDataRepository<AssessmentSalesCategory> categoryRepository,
        IGenericDataRepository<AssessmentStock> stockRepository,
        IGenericDataRepository<AssessmentManufacturingMaterial> manufacturingMaterialRepository,
        IGenericDataRepository<AssessmentManufacturingBalances> manufacturingBalancesRepository,
        IGenericDataRepository<AssessmentSales> salesRepository,
        IGenericDataRepository<AssessmentExpenses> expenseRepository,
        IGenericDataRepository<AssessmentLoan> loanRepository,
        IGenericDataRepository<AssessmentAsset> assetRepository,
        IGenericDataRepository<DebtorsCreditorsProfile> profileRepository)
    {
        _assessmentRepository = assessmentRepository;
        _assumptionsRepository = assumptionsRepository;
        _categoryRepository = categoryRepository;
        _stockRepository = stockRepository;
        _manufacturingMaterialRepository = manufacturingMaterialRepository;
        _manufacturingBalancesRepository = manufacturingBalancesRepository;
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
        var stockTask = _stockRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var manufacturingMaterialsTask = _manufacturingMaterialRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var manufacturingBalancesTask = _manufacturingBalancesRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var salesTask = _salesRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var expensesTask = _expenseRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var loansTask = _loanRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var assetsTask = _assetRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);
        var profilesTask = _profileRepository.GetAllAsync(item => item.AssessmentId == assessmentId && item.Active);

        await Task.WhenAll(
            assessmentTask, assumptionsTask, categoriesTask, stockTask,
            manufacturingMaterialsTask, manufacturingBalancesTask, salesTask,
            expensesTask, loansTask, assetsTask, profilesTask);

        var assessment = await assessmentTask;
        var assumptions = (await assumptionsTask)
            .OrderByDescending(item => item.AssessmentProjectionAssumptionsId)
            .FirstOrDefault();
        var categories = (await categoriesTask).ToArray();
        var stockItems = (await stockTask).ToArray();
        var manufacturingMaterials = (await manufacturingMaterialsTask).ToArray();
        var manufacturingBalances = (await manufacturingBalancesTask).ToArray();
        var sales = (await salesTask).ToArray();
        var expenses = (await expensesTask).ToArray();
        var loans = (await loansTask).ToArray();
        var assets = (await assetsTask).ToArray();
        var profiles = (await profilesTask).ToArray();

        var hasOpeningPosition = assessment is not null
            && (assessment.OpeningBalance_Bank != 0m
                || assessment.OpeningBalance_Assets != 0m
                || stockItems.Any(item => item.OpeningBalance != 0m)
                || manufacturingMaterials.Any(item => item.OpeningValue != 0m)
                || manufacturingBalances.Any(balance =>
                    balance.OpeningWorkInProgress != 0m
                    || balance.OpeningFinishedGoods != 0m)
                || categories.Any(category =>
                    category.OpeningDebtorsAmount != 0m
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
        var directorWagesRecorded = expenses.Any(expense => expense.IsDirectorWages);
        var legacyDirectorWagesRecorded =
            (assessment?.MonthlyDirectorWagesAmountTotal ?? 0m) > 0m;
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
            new("Manufacturing raw materials",
                assessment?.AssessmentModeId != (int)AssessmentMode.Manufacturing
                    || manufacturingMaterials.Length > 0,
                assessment?.AssessmentModeId == (int)AssessmentMode.Manufacturing
                    ? $"{manufacturingMaterials.Length} active raw-material item(s) configured."
                    : "Not required for this assessment mode.",
                "Add raw-material items with opening balances and monthly purchase/closing quantities.", true),
            new("Manufacturing inventory balances",
                assessment?.AssessmentModeId != (int)AssessmentMode.Manufacturing
                    || categories.Length > 0
                        && categories.All(category => manufacturingBalances.Any(balance =>
                        balance.AssessmentSalesCategoryId == category.AssessmentSalesCategoryId
                        && balance.InventoryEstimatesConfirmed)),
                assessment?.AssessmentModeId == (int)AssessmentMode.Manufacturing
                    ? categories.Length > 0
                        && categories.All(category => manufacturingBalances.Any(balance =>
                        balance.AssessmentSalesCategoryId == category.AssessmentSalesCategoryId
                        && balance.InventoryEstimatesConfirmed))
                        ? "Opening and monthly closing WIP/finished-goods balances are confirmed for every sales category."
                        : "One or more sales categories have missing or unconfirmed WIP/finished-goods balances."
                    : "Not required for this assessment mode.",
                "Enter and confirm opening WIP/finished goods and monthly closing inventory estimates for each sales category.", true),
            new("Debtor and creditor profile", profileValid,
                profileValid ? "Collection and payment profiles are available." : "The profile is missing or ageing buckets exceed 100%.",
                "Complete the debtor and creditor profile.", true),
            new("Operating expenses", expenses.Length > 0,
                $"{expenses.Length} active expense line(s) found.",
                "Enter the operating expenses required to run the business.", true),
            new("Director wages", directorWagesRecorded || legacyDirectorWagesRecorded,
                directorWagesRecorded
                    ? "Director wages are recorded in the Expenses module."
                    : legacyDirectorWagesRecorded
                        ? "Legacy director-wage data is present; move it to an expense line when updating this assessment."
                        : "No director-wage expense line is recorded.",
                "Add one Director Wages expense line in Expenses. Enter zero if the business has no director remuneration.", true),
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
