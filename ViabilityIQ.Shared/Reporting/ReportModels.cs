namespace ViabilityIQ.Shared.Reporting;

public enum ReportScope { Assessment, System }
public enum ReportType { AssessmentSummary, ProfitAndLoss, Cashflow, BalanceSheet }
public enum ReportOutputFormat { Pdf, Excel, Html }
public enum ReportParameterKind { MonthRange, DateRange, Text, Number }

public sealed record ReportParameterDefinition(
    string Code, string Name, ReportParameterKind Kind, bool Required, string? DefaultValue = null);

public sealed record ReportDefinition(
    ReportScope Scope, ReportType Type, string Code, int Version, string Category,
    string Name, string Description, IReadOnlySet<ReportOutputFormat> SupportedFormats,
    IReadOnlyList<ReportParameterDefinition> Parameters, bool Landscape);

public sealed class ReportDocument
{
    public required ReportDefinition Definition { get; init; }
    public required long AssessmentId { get; init; }
    public required string AssessmentReference { get; init; }
    public string? EntityName { get; init; }
    public DateTime? AssessmentStartDate { get; init; }
    public required string ReportingPeriod { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
    public required string ReadinessStatus { get; init; }
    public string? ReadinessWarning { get; init; }
    public IReadOnlyList<ReportSection> Sections { get; init; } = [];
    public IReadOnlyList<string> Footnotes { get; init; } = [];
}

public sealed class ReportSection
{
    public required string Title { get; init; }
    public string? Narrative { get; init; }
    public IReadOnlyList<ReportMetric> Metrics { get; init; } = [];
    public ReportTable? Table { get; init; }
    public IReadOnlyList<ReportFinding> Findings { get; init; } = [];
}

public sealed record ReportMetric(string Label, string DisplayValue, decimal? NumericValue = null);
public sealed class ReportTable
{
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<ReportTableRow> Rows { get; init; }
}
public sealed record ReportTableRow(
    string Label, IReadOnlyList<decimal?> Values, bool IsHeading = false,
    bool IsTotal = false, bool IsPercentage = false);
public sealed record ReportFinding(
    int Priority, string Severity, string Title, string Evidence, string Recommendation);
