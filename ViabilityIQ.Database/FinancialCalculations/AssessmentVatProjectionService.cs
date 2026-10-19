using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class AssessmentVatProjectionService : IAssessmentVatProjectionService
{
    private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
    private readonly IGenericDataRepository<Assessment> _assessmentRepository;
    private readonly IGenericDataRepository<AssessmentStock> _stockRepository;
    private readonly IGenericDataRepository<AssessmentManufacturingMaterial> _manufacturingMaterialRepository;
    private readonly IGenericDataRepository<AssessmentExpenses> _expenseRepository;
    private readonly IGenericDataRepository<AssessmentVATTransactions> _adjustmentRepository;
    private readonly ILogger<AssessmentVatProjectionService> _logger;

    public AssessmentVatProjectionService(
        IGenericDataRepository<Assessment> assessmentRepository,
        IGenericDataRepository<AssessmentSales> salesRepository,
        IGenericDataRepository<AssessmentStock> stockRepository,
        IGenericDataRepository<AssessmentManufacturingMaterial> manufacturingMaterialRepository,
        IGenericDataRepository<AssessmentExpenses> expenseRepository,
        IGenericDataRepository<AssessmentVATTransactions> adjustmentRepository,
        ILogger<AssessmentVatProjectionService> logger)
    {
        _assessmentRepository = assessmentRepository;
        _salesRepository = salesRepository;
        _stockRepository = stockRepository;
        _manufacturingMaterialRepository = manufacturingMaterialRepository;
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
            var assessment = await _assessmentRepository.GetByIdAsync(assessmentId)
                ?? throw new InvalidOperationException($"Assessment {assessmentId} was not found.");
            if (!Enum.IsDefined(typeof(AssessmentMode), assessment.AssessmentModeId))
                throw new InvalidOperationException($"Assessment mode {assessment.AssessmentModeId} is invalid.");
            var mode = (AssessmentMode)assessment.AssessmentModeId;

            var salesTask = _salesRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var stockTask = _stockRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var manufacturingMaterialsTask = _manufacturingMaterialRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var expensesTask = _expenseRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId && x.Active);
            var adjustmentsTask = _adjustmentRepository.GetAllAsync(x =>
                x.AssessmentId == assessmentId
                && x.Active
                && (x.SourceType.Equals("Adjustment", StringComparison.OrdinalIgnoreCase)
                    || x.SourceType.Equals("Manual", StringComparison.OrdinalIgnoreCase)));

            await Task.WhenAll(salesTask, stockTask, manufacturingMaterialsTask, expensesTask, adjustmentsTask);

            var sales = (await salesTask).ToList();
            var stock = (await stockTask).ToList();
            var manufacturingMaterials = (await manufacturingMaterialsTask).ToList();
            var expenses = (await expensesTask).ToList();
            var adjustments = (await adjustmentsTask).ToList();
            var result = new List<MonthlyVatProjection>(12);

            for (var period = 1; period <= 12; period++)
            {
                var index = period - 1;
                var taxableSales = sales
                    .Where(x => x.IncludeVAT != 0m)
                    .Sum(x => MonthValue(x.MonthlyValues, index));
                var taxableStock = mode switch
                {
                    AssessmentMode.Service or AssessmentMode.ManufacturingSimplified => 0m,
                    AssessmentMode.Manufacturing => manufacturingMaterials
                        .Where(x => x.IncludeVAT)
                        .Sum(x => MonthValue(x.MonthlyPurchaseValues, index)),
                    _ => stock.Where(x => x.blIncludeVAT).Sum(x => StockValue(x, mode, index))
                };
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
                        .Where(x =>
                            x.blIncludeVAT
                            && mode is not AssessmentMode.Service and not AssessmentMode.ManufacturingSimplified and not AssessmentMode.Manufacturing)
                        .Sum(x => VatAmount(
                            StockValue(x, mode, index),
                            x.TotalNoVAT,
                            x.TotalWithVAT,
                            0m))
                        + (mode == AssessmentMode.Manufacturing
                            ? manufacturingMaterials
                                .Where(x => x.IncludeVAT)
                                .Sum(x => VatAmount(
                                    MonthValue(x.MonthlyPurchaseValues, index),
                                    x.TotalNoVAT,
                                    x.TotalWithVAT,
                                    x.VATRate))
                            : 0m),
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

    private static decimal StockValue(AssessmentStock item, AssessmentMode mode, int monthIndex) =>
        mode == AssessmentMode.ManufacturingSimplified
            ? item.MonthlyProductionUnits[monthIndex] * item.StandardUnitCost
            : MonthValue(item.MonthlyValues, monthIndex);

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
