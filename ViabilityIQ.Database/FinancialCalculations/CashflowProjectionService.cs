using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class CashflowProjectionService : ICashflowProjectionService
{
    private readonly IGenericDataRepository<Assessment> _assessments;
    private readonly IGenericDataRepository<AssessmentSalesCategory> _categories;
    private readonly IGenericDataRepository<AssessmentProjectionAssumptions> _assumptions;
    private readonly IGenericDataRepository<AssessmentSales> _sales;
    private readonly IGenericDataRepository<IncomeType> _incomeTypes;
    private readonly IGenericDataRepository<AssessmentStock> _stock;
    private readonly IGenericDataRepository<AssessmentManufacturingMaterial> _manufacturingMaterials;
    private readonly IGenericDataRepository<AssessmentManufacturingBalances> _manufacturingBalances;
    private readonly IGenericDataRepository<AssessmentExpenses> _expenses;
    private readonly IAssessmentVatProjectionService _vatProjection;
    private readonly IGenericDataRepository<AssessmentLoan> _loans;
    private readonly IGenericDataRepository<AssessmentLoanRepayment> _repayments;
    private readonly IGenericDataRepository<AssessmentAssetMovement> _movements;
    private readonly IGenericDataRepository<AssessmentAsset> _assets;
    private readonly IAccountsProjectionService _accounts;
    private readonly IAssetMovementCalculationService _assetMovementCalculation;
    private readonly ILogger<CashflowProjectionService> _logger;

    public CashflowProjectionService(
        IGenericDataRepository<Assessment> assessments,
        IGenericDataRepository<AssessmentSalesCategory> categories,
        IGenericDataRepository<AssessmentProjectionAssumptions> assumptions,
        IGenericDataRepository<AssessmentSales> sales,
        IGenericDataRepository<IncomeType> incomeTypes,
        IGenericDataRepository<AssessmentStock> stock,
        IGenericDataRepository<AssessmentManufacturingMaterial> manufacturingMaterials,
        IGenericDataRepository<AssessmentManufacturingBalances> manufacturingBalances,
        IGenericDataRepository<AssessmentExpenses> expenses,
        IAssessmentVatProjectionService vatProjection,
        IGenericDataRepository<AssessmentLoan> loans,
        IGenericDataRepository<AssessmentLoanRepayment> repayments,
        IGenericDataRepository<AssessmentAssetMovement> movements,
        IGenericDataRepository<AssessmentAsset> assets,
        IAccountsProjectionService accounts,
        IAssetMovementCalculationService assetMovementCalculation,
        ILogger<CashflowProjectionService> logger)
    {
        _assessments = assessments;
        _categories = categories;
        _assumptions = assumptions;
        _sales = sales;
        _incomeTypes = incomeTypes;
        _stock = stock;
        _manufacturingMaterials = manufacturingMaterials;
        _manufacturingBalances = manufacturingBalances;
        _expenses = expenses;
        _vatProjection = vatProjection;
        _loans = loans;
        _repayments = repayments;
        _movements = movements;
        _assets = assets;
        _accounts = accounts;
        _assetMovementCalculation = assetMovementCalculation;
        _logger = logger;
    }

    public async Task<CashflowProjectionResult> CalculateAsync(long assessmentId)
    {
        try
        {
            var assessment = await _assessments.GetByIdAsync(assessmentId)
                ?? throw new InvalidOperationException($"Assessment {assessmentId} was not found.");
            if (!assessment.Active)
            {
                throw new InvalidOperationException($"Assessment {assessmentId} is inactive.");
            }

            var categoriesTask = _categories.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var assumptionsTask = _assumptions.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var salesTask = _sales.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var incomeTypesTask = _incomeTypes.GetAllAsync();
            var stockTask = _stock.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var manufacturingMaterialsTask = _manufacturingMaterials.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var manufacturingBalancesTask = _manufacturingBalances.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var expensesTask = _expenses.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var vatTask = _vatProjection.CalculateAsync(assessmentId);
            var loansTask = _loans.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var repaymentsTask = _repayments.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var movementsTask = _movements.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var assetsTask = _assets.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var accountsTask = _accounts.CalculateAsync(assessmentId);

            await Task.WhenAll(
                categoriesTask,
                assumptionsTask,
                salesTask,
                incomeTypesTask,
                stockTask,
                manufacturingMaterialsTask,
                manufacturingBalancesTask,
                expensesTask,
                vatTask,
                loansTask,
                repaymentsTask,
                movementsTask,
                assetsTask,
                accountsTask);

            var assets = (await assetsTask).ToList();
            var storedMovements = (await movementsTask)
                .GroupBy(movement => movement.AssessmentAssetId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(movement => movement.ModifiedDate).First());
            var assetMovements = new List<AssessmentAssetMovement>(assets.Count);
            foreach (var asset in assets)
            {
                var movement = storedMovements.TryGetValue(asset.AssessmentAssetId, out var savedMovement)
                    ? savedMovement
                    : await _assetMovementCalculation.InitializeMovementTemplateAsync(asset);
                assetMovements.Add(await _assetMovementCalculation.RecalculateAllMonthsAsync(movement, asset));
            }

            var depreciation = Enumerable.Range(1, 12).ToDictionary(
                month => month,
                month => assetMovements.Sum(movement => movement.GetDepreciation(month)));

            return CashflowProjectionCalculator.Calculate(new CashflowCalculationInput(
                assessment,
                (await categoriesTask).ToList(),
                (await assumptionsTask)
                    .OrderByDescending(x => x.AssessmentProjectionAssumptionsId)
                    .FirstOrDefault()?.CostOfSalesPercentage,
                (await salesTask).ToList(),
                (await incomeTypesTask).ToList(),
                (await stockTask).ToList(),
                (await manufacturingMaterialsTask).ToList(),
                (await manufacturingBalancesTask)
                    .ToList(),
                (await expensesTask).ToList(),
                await vatTask,
                (await loansTask).ToList(),
                (await repaymentsTask).ToList(),
                assetMovements,
                depreciation,
                await accountsTask));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Cashflow projection failed for assessment {AssessmentId}",
                assessmentId);
            throw;
        }
    }
}
