using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class AssessmentVatProjectionService : IAssessmentVatProjectionService
{
    private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
    private readonly IGenericDataRepository<AssessmentStock> _stockRepository;
    private readonly IGenericDataRepository<AssessmentExpenses> _expenseRepository;
    private readonly IGenericDataRepository<AssessmentVATTransactions> _adjustmentRepository;
    private readonly ILogger<AssessmentVatProjectionService> _logger;

    public AssessmentVatProjectionService(
        IGenericDataRepository<AssessmentSales> salesRepository,
        IGenericDataRepository<AssessmentStock> stockRepository,
        IGenericDataRepository<AssessmentExpenses> expenseRepository,
        IGenericDataRepository<AssessmentVATTransactions> adjustmentRepository,
        ILogger<AssessmentVatProjectionService> logger)
    {
        _salesRepository = salesRepository;
        _stockRepository = stockRepository;
        _expenseRepository = expenseRepository;
        _adjustmentRepository = adjustmentRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MonthlyVatProjection>> CalculateAsync(long assessmentId)
    {
        if (assessmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assessmentId));
        }

        try
        {
            var salesTask = _salesRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var stockTask = _stockRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var expensesTask = _expenseRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var adjustmentsTask = _adjustmentRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId
                && x.Active
                && (x.SourceType.Equals("Adjustment", StringComparison.OrdinalIgnoreCase)
                    || x.SourceType.Equals("Manual", StringComparison.OrdinalIgnoreCase)));

            await Task.WhenAll(salesTask, stockTask, expensesTask, adjustmentsTask);

            var sales = (await salesTask).ToList();
            var stock = (await stockTask).ToList();
            var expenses = (await expensesTask).ToList();
            var adjustments = (await adjustmentsTask).ToList();
            var result = new List<MonthlyVatProjection>(12);

            for (var period = 1; period <= 12; period++)
            {
                var index = period - 1;
                var taxableSales = sales
                    .Where(x => x.IncludeVAT != 0m)
                    .Sum(x => MonthValue(x.MonthlyValues, index));
                var taxableStock = stock
                    .Where(x => x.blIncludeVAT)
                    .Sum(x => MonthValue(x.MonthlyValues, index));
                var taxableExpenses = expenses
                    .Where(x => x.TotalWithVAT > x.TotalNoVAT)
                    .Sum(x => MonthValue(x.MonthlyValues, index));
                var periodAdjustments = adjustments.Where(x => x.Period == period).ToList();

                result.Add(new MonthlyVatProjection
                {
                    Period = period,
                    TaxableSales = taxableSales,
                    TaxablePurchases = taxableStock + taxableExpenses,
                    OutputVat = sales
                        .Where(x => x.IncludeVAT != 0m)
                        .Sum(x => VatAmount(
                            MonthValue(x.MonthlyValues, index),
                            x.TotalNoVAT,
                            x.TotalWithVAT,
                            x.VATRate)),
                    StockInputVat = stock
                        .Where(x => x.blIncludeVAT)
                        .Sum(x => VatAmount(
                            MonthValue(x.MonthlyValues, index),
                            x.TotalNoVAT,
                            x.TotalWithVAT,
                            0m)),
                    ExpenseInputVat = expenses
                        .Where(x => x.TotalWithVAT > x.TotalNoVAT)
                        .Sum(x => VatAmount(
                            MonthValue(x.MonthlyValues, index),
                            x.TotalNoVAT,
                            x.TotalWithVAT,
                            0m)),
                    AdjustmentOutputVat = periodAdjustments.Sum(x => x.OutputVAT),
                    AdjustmentInputVat = periodAdjustments.Sum(x => x.InputVAT)
                });
            }

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to calculate VAT projection for assessment {AssessmentId}",
                assessmentId);
            throw;
        }
    }

    private static decimal MonthValue(decimal[] values, int index) =>
        values.Length > index ? values[index] : 0m;

    private static decimal VatAmount(
        decimal monthlyNet,
        decimal annualNet,
        decimal annualGross,
        decimal fallbackRate)
    {
        if (monthlyNet == 0m)
        {
            return 0m;
        }

        if (annualNet > 0m && annualGross >= annualNet)
        {
            return monthlyNet * (annualGross - annualNet) / annualNet;
        }

        return fallbackRate > 0m
            ? monthlyNet * fallbackRate / 100m
            : 0m;
    }
}
