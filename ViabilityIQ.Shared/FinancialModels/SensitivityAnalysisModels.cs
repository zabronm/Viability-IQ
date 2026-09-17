namespace ViabilityIQ.Shared.FinancialModels;

public enum SensitivityDriver
{
    RevenuePercent,
    CostOfSalesRelativePercent,
    CostOfSalesPercentagePoints,
    OperatingExpensesPercent,
    BadDebtPercentagePoints,
    DebtorCollectionShiftMonths,
    CreditorPaymentShiftMonths,
    LoanInterestPercent,
    AssetPurchasesPercent,
    MinimumCashReserve
}

public enum SensitivityStatus { Green, Amber, Red }

public sealed record ScenarioAdjustment(
    SensitivityDriver Driver,
    decimal Value,
    int EffectiveStartMonth = 1,
    int? DurationMonths = null)
{
    public bool AppliesTo(int month) =>
        month >= EffectiveStartMonth
        && (!DurationMonths.HasValue || month < EffectiveStartMonth + DurationMonths.Value);
}

public sealed class SensitivityScenario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Custom";
    public string Description { get; set; } = string.Empty;
    public bool IsBaseline { get; init; }
    public List<ScenarioAdjustment> Adjustments { get; init; } = [];
}

public sealed record SensitivitySweepRequest(
    SensitivityDriver Driver,
    decimal Minimum,
    decimal Maximum,
    decimal Step,
    int EffectiveStartMonth = 1,
    int? DurationMonths = null);

public sealed class SensitivityMetrics
{
    public decimal Revenue { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal? GrossMarginPercent { get; init; }
    public decimal EBITDA { get; init; }
    public decimal ProfitBeforeTax { get; init; }
    public decimal ClosingCash { get; init; }
    public decimal MinimumCash { get; init; }
    public int MinimumCashMonth { get; init; }
    public decimal FundingShortfall { get; init; }
    public decimal? CurrentRatio { get; init; }
    public decimal? InterestCover { get; init; }
    public decimal? BreakEvenSales { get; init; }
    public decimal? MarginOfSafety { get; init; }
}

public sealed class ScenarioAnalysisResult
{
    public required SensitivityScenario Scenario { get; init; }
    public required CashflowProjectionResult Projection { get; init; }
    public required SensitivityMetrics Metrics { get; init; }
    public IReadOnlyList<SensitivityFinding> Findings { get; init; } = [];
    public IReadOnlyList<string> Explanations { get; init; } = [];
    public IReadOnlyList<string> CalculationNotes { get; init; } = [];
}

public sealed record SensitivitySweepPoint(
    decimal DriverValue,
    SensitivityMetrics Metrics,
    SensitivityStatus Status,
    IReadOnlyList<string> Flags);

public sealed class SensitivitySweepResult
{
    public required SensitivitySweepRequest Request { get; init; }
    public required SensitivityMetrics Baseline { get; init; }
    public IReadOnlyList<SensitivitySweepPoint> Points { get; init; } = [];
}

public sealed record SensitivityFinding(
    SensitivityStatus Status,
    int Priority,
    string Title,
    string Detail,
    string RecommendedAction);

public sealed record SensitivityThreshold(
    string Name,
    decimal? Value,
    string Unit,
    string Interpretation,
    SensitivityStatus Status);

public sealed class SensitivityRiskResult
{
    public IReadOnlyList<SensitivityThreshold> Thresholds { get; init; } = [];
    public IReadOnlyList<SensitivityFinding> Findings { get; init; } = [];
}
