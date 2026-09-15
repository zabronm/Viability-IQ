using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class CashflowProjectionService : ICashflowProjectionService
{
    private readonly IGenericDataRepository<Assessment> _assessments;
    private readonly IGenericDataRepository<AssessmentSalesCategory> _categories;
    private readonly IGenericDataRepository<AssessmentSales> _sales;
    private readonly IGenericDataRepository<IncomeType> _incomeTypes;
    private readonly IGenericDataRepository<AssessmentStock> _stock;
    private readonly IGenericDataRepository<AssessmentExpenses> _expenses;
    private readonly IAssessmentVatProjectionService _vatProjection;
    private readonly IGenericDataRepository<AssessmentLoan> _loans;
    private readonly IGenericDataRepository<AssessmentLoanRepayment> _repayments;
    private readonly IGenericDataRepository<AssessmentAssetMovement> _movements;
    private readonly IAccountsProjectionService _accounts;
    private readonly IAssetDepreciationEngine _depreciation;
    private readonly ILogger<CashflowProjectionService> _logger;

    public CashflowProjectionService(
        IGenericDataRepository<Assessment> assessments,
        IGenericDataRepository<AssessmentSalesCategory> categories,
        IGenericDataRepository<AssessmentSales> sales,
        IGenericDataRepository<IncomeType> incomeTypes,
        IGenericDataRepository<AssessmentStock> stock,
        IGenericDataRepository<AssessmentExpenses> expenses,
        IAssessmentVatProjectionService vatProjection,
        IGenericDataRepository<AssessmentLoan> loans,
        IGenericDataRepository<AssessmentLoanRepayment> repayments,
        IGenericDataRepository<AssessmentAssetMovement> movements,
        IAccountsProjectionService accounts,
        IAssetDepreciationEngine depreciation,
        ILogger<CashflowProjectionService> logger)
    {
        _assessments = assessments;
        _categories = categories;
        _sales = sales;
        _incomeTypes = incomeTypes;
        _stock = stock;
        _expenses = expenses;
        _vatProjection = vatProjection;
        _loans = loans;
        _repayments = repayments;
        _movements = movements;
        _accounts = accounts;
        _depreciation = depreciation;
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
            var salesTask = _sales.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var incomeTypesTask = _incomeTypes.GetAllAsync();
            var stockTask = _stock.GetAllAsync(x =>
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
            var accountsTask = _accounts.CalculateAsync(assessmentId);
            var depreciationTask = _depreciation.CalculateAnnualDepreciationAsync(assessmentId);

            await Task.WhenAll(
                categoriesTask,
                salesTask,
                incomeTypesTask,
                stockTask,
                expensesTask,
                vatTask,
                loansTask,
                repaymentsTask,
                movementsTask,
                accountsTask,
                depreciationTask);

            return CashflowProjectionCalculator.Calculate(new CashflowCalculationInput(
                assessment,
                (await categoriesTask).ToList(),
                (await salesTask).ToList(),
                (await incomeTypesTask).ToList(),
                (await stockTask).ToList(),
                (await expensesTask).ToList(),
                await vatTask,
                (await loansTask).ToList(),
                (await repaymentsTask).ToList(),
                (await movementsTask).ToList(),
                await depreciationTask,
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
