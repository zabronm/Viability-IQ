namespace ViabilityIQ.Shared.FinancialModels;

public enum SensitivityAiAnalysisStatus
{
    Success,
    Disabled,
    Unconfigured,
    ConsentRequired,
    Timeout,
    Unauthorized,
    RateLimited,
    ServiceUnavailable,
    SafetyBlocked,
    InvalidResponse,
    Cancelled
}

public sealed record SensitivityAiConfiguration(
    bool Enabled,
    bool Configured,
    string Provider,
    string Model,
    int MaxFindings);

public sealed class SensitivityAiAnalysisRequest
{
    public required string CaseType { get; init; }
    public required SensitivityAiMetricSnapshot Metrics { get; init; }
    public required SensitivityAiMetricSnapshot BaselineMetrics { get; init; }
    public IReadOnlyList<SensitivityAiMonthSnapshot> Months { get; init; } = [];
    public IReadOnlyList<SensitivityAiAdjustmentSnapshot> Adjustments { get; init; } = [];
    public IReadOnlyList<string> DeterministicFlags { get; init; } = [];
}

public sealed record SensitivityAiMetricSnapshot(
    decimal Revenue,
    decimal GrossProfit,
    decimal? GrossMarginPercent,
    decimal EBITDA,
    decimal ProfitBeforeTax,
    decimal ClosingCash,
    decimal MinimumCash,
    int MinimumCashMonth,
    decimal FundingShortfall,
    decimal? CurrentRatio,
    decimal? InterestCover,
    decimal? BreakEvenSales,
    decimal? MarginOfSafety);

public sealed record SensitivityAiMonthSnapshot(
    int Month,
    decimal Revenue,
    decimal ProfitBeforeTax,
    decimal ClosingCash,
    decimal NetCashflow,
    decimal ClosingDebtors,
    decimal ClosingCreditors);

public sealed record SensitivityAiAdjustmentSnapshot(
    string Driver,
    decimal Value,
    int EffectiveStartMonth,
    int? DurationMonths);

public sealed class SensitivityAiAnalysisResult
{
    public required SensitivityAiAnalysisStatus Status { get; init; }
    public required string UserMessage { get; init; }
    public string Provider { get; init; } = "Google Gemini";
    public string Model { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset? GeneratedAtUtc { get; init; }
    public string? ExecutiveSummary { get; init; }
    public IReadOnlyList<SensitivityAiFinding> Findings { get; init; } = [];
    public bool IsSuccess => Status == SensitivityAiAnalysisStatus.Success;
}

public sealed record SensitivityAiFinding(
    int Priority,
    string Area,
    string Severity,
    string Finding,
    string AuthoritativeEvidence,
    string RecommendedAction,
    int Confidence);
