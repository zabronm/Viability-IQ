namespace ViabilityIQ.Shared.Reporting;

public static class ReportCatalogue
{
    private static readonly IReadOnlySet<ReportOutputFormat> StandardOutputs =
        new HashSet<ReportOutputFormat>
        {
            ReportOutputFormat.Pdf, ReportOutputFormat.Excel, ReportOutputFormat.Html
        };

    public static IReadOnlyList<ReportDefinition> AssessmentReports { get; } =
    [
        Define(ReportType.AssessmentSummary, "ASSESSMENT_SUMMARY", "Planning & viability",
            "Assessment Summary", "Narrative critical planning report with deterministic financial findings.", false),
        Define(ReportType.ProfitAndLoss, "PROFIT_AND_LOSS", "Financial statements",
            "Profit & Loss", "Monthly projected income statement from the central projection.", true),
        Define(ReportType.Cashflow, "CASHFLOW", "Financial statements",
            "Cashflow", "Monthly projected receipts, payments and bank position.", true),
        Define(ReportType.BalanceSheet, "BALANCE_SHEET", "Financial statements",
            "Balance Sheet", "Monthly projected assets, liabilities and residual net assets.", true),
        Define(ReportType.Vat, "VAT_REPORT", "Tax reports",
            "VAT Report", "Monthly projected output VAT, input VAT and settlement position.", true)
    ];

    public static ReportDefinition Get(ReportType type) =>
        AssessmentReports.Single(report => report.Type == type);

    private static ReportDefinition Define(
        ReportType type, string code, string category, string name,
        string description, bool landscape) =>
        new(ReportScope.Assessment, type, code, 1, category, name, description,
            StandardOutputs,
            [new("MONTH_RANGE", "Projection period", ReportParameterKind.MonthRange, true, "1-12")],
            landscape);
}
