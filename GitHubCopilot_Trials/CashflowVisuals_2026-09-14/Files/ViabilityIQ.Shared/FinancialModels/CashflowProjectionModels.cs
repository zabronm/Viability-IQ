namespace ViabilityIQ.Shared.FinancialModels;

public sealed class CashflowProjectionResult
{
    public long AssessmentId { get; init; }
    public IReadOnlyList<CashflowProjectionMonth> Months { get; init; } = [];
    public CashflowProjectionSummary Summary { get; init; } = new();
    public CashflowViabilityMetrics Viability { get; init; } = new();
    public IReadOnlyList<CashflowProjectionAlert> Alerts { get; init; } = [];
    public DateTime CalculatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class CashflowProjectionMonth
{
    public int MonthNumber { get; init; }
    public string MonthLabel => $"M{MonthNumber}";
    public decimal Revenue { get; init; }
    public decimal StockPurchases { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit => Revenue - CostOfGoodsSold;
    public decimal OperatingExpenses { get; init; }
    public decimal EBITDA => GrossProfit - OperatingExpenses;
    public decimal Depreciation { get; init; }
    public decimal EBIT => EBITDA - Depreciation;
    public decimal InterestExpense { get; init; }
    public decimal ProfitBeforeTax => EBIT - InterestExpense;
    public decimal SalesReceipts { get; init; }
    public decimal SupplierPayments { get; init; }
    public decimal CashOperatingExpenses { get; init; }
    public decimal VatOutput { get; init; }
    public decimal VatInput { get; init; }
    public decimal VatPayable { get; init; }
    public decimal VatPayment => Math.Max(0m, VatPayable);
    public decimal VatRefund => Math.Max(0m, -VatPayable);
    public decimal ExpectedLoanRepayment { get; init; }
    public decimal ExtraLoanRepayment { get; init; }
    public decimal LoanCashPayment => ExpectedLoanRepayment + ExtraLoanRepayment;
    public decimal OverdraftInterest { get; init; }
    public decimal PrincipalRepaid { get; init; }
    public decimal AssetPurchases { get; init; }
    public decimal AssetDisposalProceeds { get; init; }
    public decimal TotalInflows => SalesReceipts + AssetDisposalProceeds + VatRefund;
    public decimal TotalOutflows =>
        SupplierPayments + CashOperatingExpenses + VatPayment + LoanCashPayment
        + OverdraftInterest + AssetPurchases;
    public decimal NetCashflow => TotalInflows - TotalOutflows;
    public decimal OpeningBank { get; init; }
    public decimal ClosingBank { get; init; }
    public decimal OpeningDebtors { get; init; }
    public decimal ClosingDebtors { get; init; }
    public decimal OpeningCreditors { get; init; }
    public decimal ClosingCreditors { get; init; }
    public decimal ClosingStock { get; init; }
    public decimal ClosingFixedAssets { get; init; }
    public decimal ClosingLoanBalance { get; init; }
    public decimal CurrentAssets => Math.Max(ClosingBank, 0m) + ClosingDebtors + ClosingStock;
    public decimal TotalAssets => CurrentAssets + ClosingFixedAssets;
    public decimal CurrentLiabilities =>
        ClosingCreditors + ClosingLoanBalance + Math.Max(-ClosingBank, 0m);
    public decimal? CurrentRatio => CurrentLiabilities == 0m ? null : CurrentAssets / CurrentLiabilities;
}

public sealed class CashflowProjectionSummary
{
    public decimal OpeningBank { get; init; }
    public decimal ClosingBank { get; init; }
    public decimal OpeningStock { get; init; }
    public decimal ClosingStock { get; init; }
    public decimal Revenue { get; init; }
    public decimal StockPurchases { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal OperatingExpenses { get; init; }
    public decimal EBITDA { get; init; }
    public decimal Depreciation { get; init; }
    public decimal EBIT { get; init; }
    public decimal InterestExpense { get; init; }
    public decimal ProfitBeforeTax { get; init; }
    public decimal VatPayable { get; init; }
    public decimal TotalInflows { get; init; }
    public decimal TotalOutflows { get; init; }
    public decimal NetCashflow { get; init; }
    public decimal MinimumClosingBank { get; init; }
    public decimal ClosingDebtors { get; init; }
    public decimal ClosingCreditors { get; init; }
    public decimal ClosingFixedAssets { get; init; }
    public decimal ClosingLoanBalance { get; init; }
    public decimal CurrentAssets { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal CurrentLiabilities { get; init; }
    public decimal? CurrentRatio { get; init; }
}

public sealed class CashflowViabilityMetrics
{
    public decimal? ContributionMarginRatio { get; init; }
    public decimal? BreakEvenSales { get; init; }
    public decimal? MonthlyBreakEvenSales =>
        BreakEvenSales.HasValue ? BreakEvenSales.Value / 12m : null;
    public decimal? MarginOfSafety { get; init; }
    public decimal? InterestCover { get; init; }
    public decimal? DebtServiceCoverageRatio { get; init; }
    public decimal NetPresentValue { get; init; }
    public decimal? ProfitVolatility { get; init; }
    public decimal? CurrentRatio { get; init; }
    public decimal? PaybackMonths { get; init; }
    public decimal? AssetTurnover { get; init; }
    public decimal? ReturnOnAssets { get; init; }
    public decimal HealthScore { get; init; }
    public decimal IndicativeBusinessValue => NetPresentValue;
}

public enum CashflowAlertSeverity { Healthy, Warning, Critical }

public sealed record CashflowProjectionAlert(
    string Code,
    CashflowAlertSeverity Severity,
    string Title,
    string Message);
