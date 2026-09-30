using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class CashflowEngine : ICashflowEngine
{
    private readonly ICashflowProjectionService _projection;
    private readonly ICashflowRepository _repository;
    private readonly ILogger<CashflowEngine> _logger;

    public CashflowEngine(
        ICashflowProjectionService projection,
        ICashflowRepository repository,
        ILogger<CashflowEngine> logger)
    {
        _projection = projection;
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<AssessmentCashflow>> CalculateMonthlyCashflowAsync(long assessmentId)
    {
        var result = await _projection.CalculateAsync(assessmentId);
        var rows = result.Months.Select(x => MapEntity(result.AssessmentId, x)).ToList();
        await _repository.SaveMonthlyCashflowAsync(rows);
        return rows;
    }

    public async Task<CashflowSummary> CalculateCashflowSummaryAsync(long assessmentId)
    {
        var result = await _projection.CalculateAsync(assessmentId);
        var summary = MapSummaryEntity(result);
        await _repository.SaveCashflowSummaryAsync(summary);
        return summary;
    }

    public async Task<List<CashflowMonthlyDto>> GetMonthlyCashflowDisplayAsync(long assessmentId)
    {
        var result = await _projection.CalculateAsync(assessmentId);
        return result.Months.Select(x => new CashflowMonthlyDto
        {
            MonthNumber = x.MonthNumber,
            MonthName = x.MonthLabel,
            SalesRevenue = x.Revenue,
            OtherIncome = x.AssetDisposalProceeds,
            TotalIncome = x.TotalInflows,
            COGS = x.CostOfGoodsSold,
            LoanRepayment = x.LoanCashPayment,
            OtherExpense = x.CashOperatingExpenses + x.SupplierPayments + x.VatPayment + x.AssetPurchases,
            TotalExpense = x.TotalOutflows,
            DepreciationExpense = x.Depreciation,
            GrossVAT = x.VatOutput,
            NetVAT = x.VatPayable,
            NetCashflow = x.NetCashflow,
            OpeningBalance = x.OpeningBank,
            ClosingBalance = x.ClosingBank,
            CumulativeCashflow = x.ClosingBank - result.Summary.OpeningBank,
            HasNegativeCashflow = x.NetCashflow < 0m,
            IsCritical = x.ClosingBank < 0m
        }).ToList();
    }

    public async Task<CashflowSummaryDto> GetCashflowSummaryDisplayAsync(long assessmentId)
    {
        var result = await _projection.CalculateAsync(assessmentId);
        var summary = result.Summary;
        return new()
        {
            TotalAnnualIncome = summary.Revenue,
            TotalAnnualExpense = summary.CostOfGoodsSold + summary.OperatingExpenses
                                 + summary.Depreciation + summary.InterestExpense,
            TotalAnnualNetCashflow = summary.NetCashflow,
            TotalAnnualDepreciation = summary.Depreciation,
            MinimumCashBalance = summary.MinimumClosingBank,
            MaximumCashBalance = result.Months.Max(x => x.ClosingBank),
            AverageMonthlyNetCashflow = result.Months.Average(x => x.NetCashflow),
            MonthsWithNegativeCashflow = result.Months.Count(x => x.NetCashflow < 0m),
            CriticalMonthsCount = result.Months.Count(x => x.ClosingBank < 0m),
            CashflowRunway = CalculateRunway(result),
            OperatingMarginRatio = Percent(summary.ProfitBeforeTax, summary.Revenue),
            ExpenseRatio = Percent(
                summary.CostOfGoodsSold + summary.OperatingExpenses + summary.Depreciation + summary.InterestExpense,
                summary.Revenue),
            HealthStatus = summary.MinimumClosingBank < 0m
                ? CashflowHealthStatus.Critical
                : result.Alerts.Any(x => x.Severity == CashflowAlertSeverity.Warning)
                    ? CashflowHealthStatus.Warning
                    : CashflowHealthStatus.Healthy,
            IsSustainable = summary.MinimumClosingBank >= 0m && summary.ProfitBeforeTax >= 0m
        };
    }

    public async Task<bool> RecalculateCashflowAsync(long assessmentId)
    {
        try
        {
            var result = await _projection.CalculateAsync(assessmentId);
            await _repository.ClearCashflowAsync(assessmentId);
            await _repository.SaveMonthlyCashflowAsync(result.Months.Select(x => MapEntity(result.AssessmentId, x)).ToList());
            await _repository.SaveCashflowSummaryAsync(MapSummaryEntity(result));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to persist compatibility cashflow for assessment {AssessmentId}", assessmentId);
            throw;
        }
    }

    private static AssessmentCashflow MapEntity(long assessmentId, CashflowProjectionMonth x) => new()
    {
        AssessmentId = assessmentId,
        MonthNumber = x.MonthNumber,
        Year = DateTime.UtcNow.Year,
        SalesRevenue = x.Revenue,
        OtherIncome = x.AssetDisposalProceeds,
        TotalIncome = x.TotalInflows,
        COGS = x.CostOfGoodsSold,
        LoanRepayment = x.LoanCashPayment,
        OtherExpense = x.CashOperatingExpenses + x.SupplierPayments + x.VatPayment + x.AssetPurchases,
        TotalExpense = x.TotalOutflows,
        DepreciationExpense = x.Depreciation,
        NetCashflow = x.NetCashflow,
        OpeningBalance = x.OpeningBank,
        ClosingBalance = x.ClosingBank,
        CumulativeCashflow = x.ClosingBank,
        HasNegativeCashflow = x.NetCashflow < 0m,
        IsCritical = x.ClosingBank < 0m,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true,
        Notes = "Compatibility row generated from authoritative live cashflow projection."
    };

    private static CashflowSummary MapSummaryEntity(CashflowProjectionResult result)
    {
        var x = result.Summary;
        var totalExpense = x.CostOfGoodsSold + x.OperatingExpenses + x.Depreciation + x.InterestExpense;
        return new()
        {
            AssessmentId = result.AssessmentId,
            TotalAnnualIncome = x.Revenue,
            TotalAnnualExpense = totalExpense,
            TotalAnnualNetCashflow = x.NetCashflow,
            MinimumCashBalance = x.MinimumClosingBank,
            MaximumCashBalance = result.Months.Max(m => m.ClosingBank),
            AverageMonthlyNetCashflow = result.Months.Average(m => m.NetCashflow),
            MonthsWithNegativeCashflow = result.Months.Count(m => m.NetCashflow < 0m),
            CriticalMonthsCount = result.Months.Count(m => m.ClosingBank < 0m),
            CashflowRunway = CalculateRunway(result),
            OperatingMarginRatio = Percent(x.ProfitBeforeTax, x.Revenue),
            ExpenseRatio = Percent(totalExpense, x.Revenue),
            HealthStatus = x.MinimumClosingBank < 0m ? CashflowHealthStatus.Critical :
                result.Alerts.Any(a => a.Severity == CashflowAlertSeverity.Warning)
                    ? CashflowHealthStatus.Warning : CashflowHealthStatus.Healthy,
            IsSustainable = x.MinimumClosingBank >= 0m && x.ProfitBeforeTax >= 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    private static decimal Percent(decimal value, decimal denominator) =>
        denominator == 0m ? 0m : value / denominator * 100m;

    private static decimal CalculateRunway(CashflowProjectionResult result)
    {
        var averageOutflow = result.Months.Average(x => x.TotalOutflows);
        return averageOutflow <= 0m ? 0m : Math.Max(0m, result.Summary.ClosingBank / averageOutflow);
    }
}
