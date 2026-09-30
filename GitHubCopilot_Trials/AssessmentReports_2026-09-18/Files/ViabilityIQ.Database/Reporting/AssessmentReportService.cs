using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.Reporting;

namespace ViabilityIQ.Application.Reporting;

public sealed class AssessmentReportService : IAssessmentReportService
{
    private readonly ICashflowProjectionService _projections;
    private readonly ISensitivityAnalysisService _sensitivity;
    private readonly IGenericDataRepository<Assessment> _assessments;
    private readonly IGenericDataRepository<Business> _businesses;
    private readonly ILogger<AssessmentReportService> _logger;

    public AssessmentReportService(
        ICashflowProjectionService projections,
        ISensitivityAnalysisService sensitivity,
        IGenericDataRepository<Assessment> assessments,
        IGenericDataRepository<Business> businesses,
        ILogger<AssessmentReportService> logger)
    {
        _projections = projections;
        _sensitivity = sensitivity;
        _assessments = assessments;
        _businesses = businesses;
        _logger = logger;
    }

    public async Task<ReportDocument> BuildAsync(
        long assessmentId, ReportType reportType, int startMonth = 1, int endMonth = 12,
        CancellationToken cancellationToken = default)
    {
        if (assessmentId <= 0) throw new ArgumentOutOfRangeException(nameof(assessmentId));
        if (startMonth is < 1 or > 12 || endMonth is < 1 or > 12 || startMonth > endMonth)
            throw new ArgumentOutOfRangeException(nameof(startMonth), "Use a valid M1-M12 range.");

        cancellationToken.ThrowIfCancellationRequested();
        var assessmentTask = _assessments.GetByIdAsync(assessmentId);
        var projectionTask = _projections.CalculateAsync(assessmentId);
        await Task.WhenAll(assessmentTask, projectionTask);

        var assessment = await assessmentTask
            ?? throw new InvalidOperationException($"Assessment {assessmentId} was not found.");
        var projection = await projectionTask;
        var months = projection.Months
            .Where(month => month.MonthNumber >= startMonth && month.MonthNumber <= endMonth)
            .OrderBy(month => month.MonthNumber)
            .ToArray();
        if (months.Length == 0)
            throw new InvalidOperationException("The projection returned no months for the selected period.");

        Business? business = null;
        if (assessment.BusinessId > 0)
            business = await _businesses.GetByIdAsync(assessment.BusinessId);

        SensitivityRiskResult? risks = null;
        if (reportType == ReportType.AssessmentSummary)
        {
            try
            {
                risks = await _sensitivity.AnalyseRisksAsync(assessmentId, 0m, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Sensitivity evidence was unavailable while generating assessment report {AssessmentId}",
                    assessmentId);
            }
        }

        var definition = ReportCatalogue.Get(reportType);
        var ready = assessment.blSales > 0 && assessment.blExpenses > 0;
        return new ReportDocument
        {
            Definition = definition,
            AssessmentId = assessmentId,
            AssessmentReference = string.IsNullOrWhiteSpace(assessment.CaseNumber)
                ? $"Assessment {assessmentId}" : assessment.CaseNumber,
            EntityName = string.IsNullOrWhiteSpace(business?.BusinessName) ? null : business.BusinessName,
            AssessmentStartDate = assessment.AssessmentStartDate,
            ReportingPeriod = ProjectPeriodMapper.GetRangeLabel(
                assessment.AssessmentStartDate, startMonth, endMonth),
            GeneratedAtUtc = DateTime.UtcNow,
            ReadinessStatus = ready ? "Projection baseline ready" : "Draft / incomplete baseline",
            ReadinessWarning = ready ? null :
                "Sales and/or expense readiness has not been confirmed. Interpret results as a draft.",
            Sections = reportType switch
            {
                ReportType.AssessmentSummary =>
                    BuildSummary(assessment, business, projection, months, risks),
                ReportType.ProfitAndLoss =>
                    [BuildProfitAndLoss(months, assessment.AssessmentStartDate)],
                ReportType.Cashflow =>
                    [BuildCashflow(months, assessment.AssessmentStartDate)],
                ReportType.BalanceSheet =>
                    [BuildBalanceSheet(months, assessment.AssessmentStartDate)],
                _ => throw new ArgumentOutOfRangeException(nameof(reportType))
            },
            Footnotes = BuildFootnotes(reportType, risks)
        };
    }

    private static IReadOnlyList<ReportSection> BuildSummary(
        Assessment assessment,
        Business? business,
        CashflowProjectionResult projection,
        IReadOnlyList<CashflowProjectionMonth> months,
        SensitivityRiskResult? risks)
    {
        var revenue = months.Sum(x => x.Revenue);
        var grossProfit = months.Sum(x => x.GrossProfit);
        var ebitda = months.Sum(x => x.EBITDA);
        var pbt = months.Sum(x => x.ProfitBeforeTax);
        var opening = months[0].OpeningBank;
        var closing = months[^1].ClosingBank;
        var minimumMonth = months.MinBy(x => x.ClosingBank)!;
        var shortfall = Math.Max(0m, -minimumMonth.ClosingBank);
        var closingMonth = months[^1];
        var grossMargin = revenue == 0m ? (decimal?)null : grossProfit / revenue;
        var netAssets = closingMonth.TotalAssets - closingMonth.CurrentLiabilities;
        var minimumPeriod = ProjectPeriodMapper.GetMonth(
            assessment.AssessmentStartDate, minimumMonth.MonthNumber).CalendarLabel;
        var status = DetermineStatus(projection, pbt, closing, closingMonth.CurrentRatio);
        var profile = BuildProfile(assessment, business);
        var findings = BuildFindings(
            projection, risks, pbt, minimumMonth, closingMonth, minimumPeriod).Take(8).ToArray();

        return
        [
            new()
            {
                Title = "Purpose and scope",
                Narrative = "This critical planning report converts management assumptions into a twelve-month " +
                    "cashflow, income statement and management assessment. It supports planning, funding and " +
                    "actual-versus-plan monitoring; it does not guarantee an outcome."
            },
            new()
            {
                Title = "Assessment and business profile",
                Narrative = profile.Count == 0
                    ? "No additional verified business profile facts were available for this report."
                    : null,
                Metrics = profile
            },
            new()
            {
                Title = "Executive assessment",
                Narrative = $"{status.Label}. {status.Detail}",
                Metrics =
                [
                    new("Deterministic status", status.Label),
                    new("Projection health score", $"{projection.Viability.HealthScore:N0}/100",
                        projection.Viability.HealthScore)
                ]
            },
            new()
            {
                Title = "Key financial results",
                Metrics =
                [
                    Money("Revenue", revenue), Money("Gross profit", grossProfit),
                    Percent("Gross margin", grossMargin), Money("EBITDA", ebitda),
                    Money("Profit before tax", pbt), Money("Closing cash", closing),
                    Money($"Lowest cash ({minimumPeriod})", minimumMonth.ClosingBank),
                    Money("Funding shortfall", shortfall),
                    Ratio("Current ratio", closingMonth.CurrentRatio),
                    Ratio("Interest cover", projection.Viability.InterestCover),
                    Ratio("DSCR", projection.Viability.DebtServiceCoverageRatio),
                    MoneyNullable("Break-even sales", projection.Viability.BreakEvenSales),
                    Percent("Margin of safety", projection.Viability.MarginOfSafety),
                    Money("Total assets", closingMonth.TotalAssets),
                    Money("Net assets", netAssets)
                ]
            },
            new()
            {
                Title = "Cash and liquidity",
                Narrative = $"Cash moves from {MoneyText(opening)} to {MoneyText(closing)}. " +
                    $"The lowest projected balance is {MoneyText(minimumMonth.ClosingBank)} in " +
                    $"{minimumPeriod}; {months.Count(x => x.ClosingBank < 0m)} month(s) are below zero. " +
                    "Monthly actuals should be compared to this path and corrective action taken before a shortfall."
            },
            new()
            {
                Title = "Profitability and operating drivers",
                Narrative = $"Projected revenue is {MoneyText(revenue)}, producing gross profit of " +
                    $"{MoneyText(grossProfit)} ({PercentText(grossMargin)} margin), EBITDA of " +
                    $"{MoneyText(ebitda)} and profit before tax of {MoneyText(pbt)}. " +
                    "Sales volume, gross margin and overhead control are the principal planning levers."
            },
            new()
            {
                Title = "Working capital",
                Narrative = $"At period end, trade debtors are {MoneyText(closingMonth.ClosingDebtors)}, " +
                    $"creditors are {MoneyText(closingMonth.ClosingCreditors)} and stock is " +
                    $"{MoneyText(closingMonth.ClosingStock)}. Collection, supplier-payment and stock timing " +
                    "can move cash materially even where accounting profit is positive."
            },
            new()
            {
                Title = "Sensitivity and break-even",
                Narrative = risks is null
                    ? "Sensitivity thresholds were unavailable. The base projection remains authoritative; rerun " +
                      "sensitivity analysis before relying on downside capacity."
                    : "Deterministic scenario thresholds test sales decline, margin/cost pressure and collection " +
                      "timing. The most material evidenced results are included in the prioritised findings.",
                Metrics = risks?.Thresholds.Take(8)
                    .Select(x => new ReportMetric(x.Name,
                        x.Value.HasValue ? $"{x.Value:N2} {x.Unit}" : "Unavailable", x.Value))
                    .ToArray() ?? []
            },
            new()
            {
                Title = "Asset and funding position",
                Narrative = $"Closing fixed assets are {MoneyText(closingMonth.ClosingFixedAssets)}, loan balances " +
                    $"are {MoneyText(closingMonth.ClosingLoanBalance)}, and total assets are " +
                    $"{MoneyText(closingMonth.TotalAssets)}. Asset turnover is " +
                    $"{RatioText(projection.Viability.AssetTurnover)}; under-used assets should be reviewed before " +
                    "new capital expenditure or borrowing."
            },
            new() { Title = "Prioritised findings and recommendations", Findings = findings },
            new()
            {
                Title = "Assumptions, readiness and source guidance",
                Narrative = "Results are calculated from the current assessment projection and sensitivity services. " +
                    "Maintain source notes for sales, costs, payment terms, opening balances, assets and borrowings; " +
                    "resolve readiness warnings and refresh this report whenever assumptions change."
            },
            new()
            {
                Title = "Responsibility statement",
                Narrative = "This management-purpose projection depends on information and assumptions supplied by " +
                    "the client and management. It is not an audit, independent assurance, tax or legal advice, and " +
                    "should not be represented as a guarantee of profitability, liquidity, funding or business value. " +
                    "Management remains responsible for decisions, records and ongoing actual-versus-plan monitoring."
            }
        ];
    }

    private static ReportSection BuildProfitAndLoss(
        IReadOnlyList<CashflowProjectionMonth> months, DateTime? assessmentStartDate) =>
        new()
        {
            Title = "Projected Profit & Loss",
            Table = Table(months, assessmentStartDate, true,
            [
                Row("Sales revenue", months, x => x.Revenue),
                Row("Sundry income", months, x => x.SundryIncome),
                Row("Grants and donations", months, x => x.GrantsDonationsIncome),
                Row("Asset disposal income", months, x => x.ClassifiedAssetDisposalIncome),
                Row("Other income", months, x => x.OtherIncome),
                Row("Total income", months, x => x.Revenue + x.AdditionalIncome, total: true),
                Row("Cost of goods sold", months, x => -x.CostOfGoodsSold),
                Row("Gross profit", months, x => x.GrossProfit, total: true),
                PercentageRow("Gross margin", months, x => x.Revenue == 0m ? null : x.GrossProfit / x.Revenue),
                Row("Operating expenses", months, x => -x.OperatingExpenses),
                Row("EBITDA", months, x => x.EBITDA, total: true),
                Row("Depreciation", months, x => -x.Depreciation),
                Row("EBIT", months, x => x.EBIT, total: true),
                Row("Interest", months, x => -x.InterestExpense),
                Row("Profit before tax", months, x => x.ProfitBeforeTax, total: true)
            ])
        };

    private static ReportSection BuildCashflow(
        IReadOnlyList<CashflowProjectionMonth> months, DateTime? assessmentStartDate) =>
        new()
        {
            Title = "Projected Cashflow",
            Table = Table(months, assessmentStartDate, true,
            [
                Row("Opening cash", months, x => x.OpeningBank, closingTotal: months[0].OpeningBank),
                Row("Sales receipts", months, x => x.SalesReceipts),
                Row("Sundry receipts", months, x => x.SundryIncomeReceipts),
                Row("Grant and donation receipts", months, x => x.GrantsDonationsReceipts),
                Row("Asset disposal receipts", months, x => x.AssetDisposalReceipts),
                Row("Other receipts", months, x => x.OtherIncomeReceipts),
                Row("VAT refunds", months, x => x.VatRefund),
                Row("Total inflows", months, x => x.TotalInflows, total: true),
                Row("Supplier payments", months, x => -x.SupplierPayments),
                Row("Operating expense payments", months, x => -x.CashOperatingExpenses),
                Row("VAT payments", months, x => -x.VatPayment),
                Row("Loan repayments", months, x => -x.LoanCashPayment),
                Row("Overdraft interest", months, x => -x.OverdraftInterest),
                Row("Asset purchases", months, x => -x.AssetPurchases),
                Row("Total outflows", months, x => -x.TotalOutflows, total: true),
                Row("Net cashflow", months, x => x.NetCashflow, total: true),
                Row("Closing cash", months, x => x.ClosingBank, total: true,
                    closingTotal: months[^1].ClosingBank)
            ])
        };

    private static ReportSection BuildBalanceSheet(
        IReadOnlyList<CashflowProjectionMonth> months, DateTime? assessmentStartDate)
    {
        decimal Liabilities(CashflowProjectionMonth x) =>
            Math.Max(-x.ClosingBank, 0m) + x.ClosingCreditors +
            x.OtherCurrentLiabilities + x.ClosingLoanBalance;
        decimal Equity(CashflowProjectionMonth x) => x.TotalAssets - Liabilities(x);

        return new()
        {
            Title = "Projected Balance Sheet",
            Table = Table(months, assessmentStartDate, false,
            [
                Heading("ASSETS"), Heading("Current assets"),
                Row("Cash at bank", months, x => Math.Max(x.ClosingBank, 0m)),
                Row("Trade debtors", months, x => x.ClosingDebtors),
                Row("Stock", months, x => x.ClosingStock),
                Row("Other current assets", months, x => x.OtherCurrentAssets),
                Row("Total current assets", months, x => x.CurrentAssets, total: true),
                Heading("Non-current assets"),
                Row("Fixed assets (net)", months, x => x.ClosingFixedAssets),
                Row("TOTAL ASSETS", months, x => x.TotalAssets, total: true),
                Heading("LIABILITIES"),
                Row("Bank overdraft", months, x => Math.Max(-x.ClosingBank, 0m)),
                Row("Trade creditors", months, x => x.ClosingCreditors),
                Row("VAT / other current liabilities", months, x => x.OtherCurrentLiabilities),
                Row("Loans and borrowings", months, x => x.ClosingLoanBalance),
                Row("TOTAL LIABILITIES", months, Liabilities, total: true),
                Heading("RESIDUAL EQUITY / NET ASSETS"),
                Row("Residual equity / net assets", months, Equity, total: true),
                Row("TOTAL LIABILITIES & EQUITY", months, x => Liabilities(x) + Equity(x), total: true),
                PercentageRow("Current ratio", months, x => x.CurrentRatio)
            ])
        };
    }

    private static ReportTable Table(
        IReadOnlyList<CashflowProjectionMonth> months, DateTime? assessmentStartDate, bool total,
        IReadOnlyList<ReportTableRow> rows) =>
        new()
        {
            Headers = total
                ? ["Line item", .. months.Select(x => ProjectPeriodMapper.GetTableLabel(
                    assessmentStartDate, x.MonthNumber)), "Total / closing"]
                : ["Line item", .. months.Select(x => ProjectPeriodMapper.GetTableLabel(
                    assessmentStartDate, x.MonthNumber))],
            Rows = total
                ? rows
                : rows.Select(row => row with
                    { Values = row.Values.Take(months.Count).ToArray() }).ToArray()
        };

    private static ReportTableRow Row(
        string label, IReadOnlyList<CashflowProjectionMonth> months,
        Func<CashflowProjectionMonth, decimal> selector, bool total = false,
        decimal? closingTotal = null) =>
        new(label,
            [.. months.Select(x => (decimal?)selector(x)),
             closingTotal ?? months.Sum(selector)],
            IsTotal: total);

    private static ReportTableRow PercentageRow(
        string label, IReadOnlyList<CashflowProjectionMonth> months,
        Func<CashflowProjectionMonth, decimal?> selector) =>
        new(label, [.. months.Select(selector), (decimal?)null], IsPercentage: true);

    private static ReportTableRow Heading(string label) => new(label, [], IsHeading: true);

    private static IReadOnlyList<ReportMetric> BuildProfile(Assessment assessment, Business? business)
    {
        var metrics = new List<ReportMetric>();
        if (business is not null)
        {
            if (!string.IsNullOrWhiteSpace(business.BusinessName))
                metrics.Add(new("Business", business.BusinessName));
            if (!string.IsNullOrWhiteSpace(business.CKNumber))
                metrics.Add(new("Registration reference", business.CKNumber));
            metrics.Add(new("Registered business", business.IsRegistered ? "Yes" : "No"));
            metrics.Add(new("VAT registered", business.IsVATRegistered ? "Yes" : "No"));
        }
        metrics.Add(new("Assessment status ID", assessment.StatusId.ToString()));
        metrics.Add(new("Assessment progress", $"{assessment.ProgressPercentage}%"));
        return metrics;
    }

    private static (string Label, string Detail) DetermineStatus(
        CashflowProjectionResult projection, decimal pbt, decimal closing, decimal? currentRatio)
    {
        if (projection.Alerts.Any(x => x.Severity == CashflowAlertSeverity.Critical)
            || closing < 0m || pbt < 0m || currentRatio is < 1m)
            return ("Material viability risks identified",
                "The base case contains a loss, liquidity deficit, weak short-term cover or critical projection alert.");
        if (projection.Alerts.Any(x => x.Severity == CashflowAlertSeverity.Warning)
            || currentRatio is < 1.5m)
            return ("Viable with active management",
                "The base case is not immediately critical, but evidenced warnings require monitoring and action.");
        return ("Base case financially supportable",
            "No material deterministic base-case trigger was identified; downside testing and monitoring remain necessary.");
    }

    private static IEnumerable<ReportFinding> BuildFindings(
        CashflowProjectionResult projection, SensitivityRiskResult? risks, decimal pbt,
        CashflowProjectionMonth minimumMonth, CashflowProjectionMonth closing,
        string minimumPeriod)
    {
        var findings = new List<ReportFinding>();
        findings.AddRange(projection.Alerts.Select(x => new ReportFinding(
            x.Severity == CashflowAlertSeverity.Critical ? 1 :
                x.Severity == CashflowAlertSeverity.Warning ? 2 : 3,
            x.Severity.ToString(), x.Title, x.Message,
            RecommendationFor(x.Code, x.Severity))));
        if (risks is not null)
            findings.AddRange(risks.Findings.Select(x => new ReportFinding(
                x.Priority, x.Status.ToString(), x.Title, x.Detail, x.RecommendedAction)));
        if (minimumMonth.ClosingBank < 0m)
            findings.Add(new(1, "Critical", "Funding gap",
                $"{minimumPeriod} closes at {MoneyText(minimumMonth.ClosingBank)}.",
                "Secure working-capital capacity before the first deficit and phase discretionary outflows."));
        if (pbt < 0m)
            findings.Add(new(1, "Critical", "Base-case loss",
                $"Projected profit before tax is {MoneyText(pbt)}.",
                "Reprice, protect gross margin and remove non-essential overhead using monthly targets."));
        if (closing.CurrentRatio is < 1.5m)
            findings.Add(new(closing.CurrentRatio is < 1m ? 1 : 2, "Warning", "Liquidity cover",
                $"Closing current ratio is {RatioText(closing.CurrentRatio)}.",
                "Accelerate collections, control stock and align supplier terms with receipt timing."));
        return findings
            .GroupBy(x => x.Title, StringComparer.OrdinalIgnoreCase).Select(x => x.First())
            .OrderBy(x => x.Priority).ThenBy(x => x.Title);
    }

    private static string RecommendationFor(string code, CashflowAlertSeverity severity) =>
        code.Contains("CASH", StringComparison.OrdinalIgnoreCase)
            ? "Prepare a month-specific cash mitigation plan and funding trigger."
            : code.Contains("PROFIT", StringComparison.OrdinalIgnoreCase)
                ? "Review price, gross margin and controllable overhead assumptions."
                : severity == CashflowAlertSeverity.Healthy
                    ? "Maintain controls and compare actual results to plan monthly."
                    : "Assign an owner, target and review date to the evidenced risk.";

    private static IReadOnlyList<string> BuildFootnotes(
        ReportType type, SensitivityRiskResult? risks)
    {
        var notes = new List<string>
        {
            "Source: authoritative live central cashflow projection at the generated timestamp.",
            "Blank or unavailable ratios indicate that the denominator was zero or the source metric was not calculable."
        };
        if (type == ReportType.BalanceSheet)
            notes.Add("Residual equity/net assets is the balancing amount after recognised projected assets and liabilities; it is not a maintained share-capital or drawings ledger.");
        if (type == ReportType.AssessmentSummary && risks is null)
            notes.Add("Sensitivity analysis was unavailable when this report was generated.");
        return notes;
    }

    private static ReportMetric Money(string label, decimal value) =>
        new(label, MoneyText(value), value);
    private static ReportMetric MoneyNullable(string label, decimal? value) =>
        new(label, value.HasValue ? MoneyText(value.Value) : "Unavailable", value);
    private static ReportMetric Percent(string label, decimal? value) =>
        new(label, PercentText(value), value);
    private static ReportMetric Ratio(string label, decimal? value) =>
        new(label, RatioText(value), value);
    private static string MoneyText(decimal value) =>
        value < 0m ? $"(R {Math.Abs(value):N0})" : $"R {value:N0}";
    private static string PercentText(decimal? value) =>
        value.HasValue ? $"{value.Value:P1}" : "Unavailable";
    private static string RatioText(decimal? value) =>
        value.HasValue ? $"{value.Value:N2}x" : "Unavailable";
}
