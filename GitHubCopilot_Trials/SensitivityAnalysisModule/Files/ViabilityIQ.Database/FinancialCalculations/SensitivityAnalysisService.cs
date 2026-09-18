using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class SensitivityAnalysisService : ISensitivityAnalysisService
{
    private readonly ICashflowProjectionService _projectionService;
    private readonly ILogger<SensitivityAnalysisService> _logger;

    public SensitivityAnalysisService(
        ICashflowProjectionService projectionService,
        ILogger<SensitivityAnalysisService> logger)
    {
        _projectionService = projectionService;
        _logger = logger;
    }

    public async Task<ScenarioAnalysisResult> CalculateScenarioAsync(
        long assessmentId,
        SensitivityScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var baseline = await _projectionService.CalculateAsync(assessmentId);
            return Calculate(baseline, scenario);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Sensitivity scenario {ScenarioName} failed for assessment {AssessmentId}",
                scenario.Name, assessmentId);
            throw;
        }
    }

    public async Task<SensitivitySweepResult> RunSweepAsync(
        long assessmentId,
        SensitivitySweepRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Step <= 0m)
            throw new ArgumentOutOfRangeException(nameof(request), "Sensitivity step must be positive.");
        if (request.Maximum < request.Minimum)
            throw new ArgumentException("Maximum must be greater than or equal to minimum.", nameof(request));
        if ((request.Maximum - request.Minimum) / request.Step > 100m)
            throw new ArgumentException("A sweep may contain no more than 101 points.", nameof(request));

        var baselineProjection = await _projectionService.CalculateAsync(assessmentId);
        var baseline = BuildMetrics(baselineProjection, 0m);
        var points = new List<SensitivitySweepPoint>();
        for (var value = request.Minimum; value <= request.Maximum; value += request.Step)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scenario = new SensitivityScenario
            {
                Name = $"{DriverLabel(request.Driver)} {value:+0.##;-0.##;0}",
                Adjustments =
                [
                    new(request.Driver, value, request.EffectiveStartMonth, request.DurationMonths)
                ]
            };
            var result = Calculate(baselineProjection, scenario);
            var flags = DetectFlags(result.Metrics, 0m);
            points.Add(new(value, result.Metrics, StatusFor(flags), flags));
        }

        return new SensitivitySweepResult
        {
            Request = request,
            Baseline = baseline,
            Points = points
        };
    }

    public async Task<SensitivityRiskResult> AnalyseRisksAsync(
        long assessmentId,
        decimal minimumCashReserve,
        CancellationToken cancellationToken = default)
    {
        var baseline = await _projectionService.CalculateAsync(assessmentId);
        cancellationToken.ThrowIfCancellationRequested();

        var salesLimit = FindThreshold(baseline, SensitivityDriver.RevenuePercent, -1m, -100m, minimumCashReserve);
        var expenseLimit = FindThreshold(baseline, SensitivityDriver.OperatingExpensesPercent, 1m, 200m, minimumCashReserve);
        var delayLimit = FindThreshold(baseline, SensitivityDriver.DebtorCollectionShiftMonths, 1m, 6m, minimumCashReserve);
        var metrics = BuildMetrics(baseline, minimumCashReserve);
        var requiredGrossMargin = baseline.Summary.Revenue == 0m
            ? null
            : (decimal?)(baseline.Summary.OperatingExpenses
                + baseline.Summary.Depreciation
                + baseline.Summary.InterestExpense) / baseline.Summary.Revenue * 100m;

        var findings = BuildFindings(metrics, minimumCashReserve);
        return new SensitivityRiskResult
        {
            Thresholds =
            [
                Threshold("Maximum sales decline", salesLimit, "%",
                    salesLimit.HasValue ? "First tested decline that causes negative profit or breaches the cash reserve." : "No breach within the tested 100% decline range."),
                Threshold("Maximum operating expense increase", expenseLimit, "%",
                    expenseLimit.HasValue ? "First tested increase that causes negative profit or breaches the cash reserve." : "No breach within the tested 200% range."),
                Threshold("Minimum gross margin", requiredGrossMargin, "%",
                    requiredGrossMargin.HasValue ? "Approximate annual margin required to cover operating expenses, depreciation and interest." : "Unavailable because baseline revenue is zero."),
                Threshold("Collection delay tolerance", delayLimit, " months",
                    delayLimit.HasValue ? "First whole-month delay that breaches the cash reserve." : "No reserve breach within the tested six-month delay."),
                Threshold("Minimum projected cash", metrics.MinimumCash, " amount",
                    $"Lowest projected closing cash occurs in M{metrics.MinimumCashMonth}.",
                    metrics.MinimumCash < minimumCashReserve ? SensitivityStatus.Red : SensitivityStatus.Green)
            ],
            Findings = findings
        };
    }

    private static ScenarioAnalysisResult Calculate(
        CashflowProjectionResult baseline,
        SensitivityScenario scenario)
    {
        if (scenario.IsBaseline || scenario.Adjustments.Count == 0)
        {
            var baselineMetrics = BuildMetrics(baseline, Reserve(scenario));
            return new()
            {
                Scenario = scenario,
                Projection = baseline,
                Metrics = baselineMetrics,
                Findings = BuildFindings(baselineMetrics, Reserve(scenario)),
                Explanations = ["This is the immutable authoritative baseline projection."],
                CalculationNotes = ["No source assessment records were changed."]
            };
        }

        var source = baseline.Months.OrderBy(x => x.MonthNumber).ToArray();
        var salesReceipts = ShiftSeries(
            source.Select(x => x.SalesReceipts).ToArray(),
            scenario,
            SensitivityDriver.DebtorCollectionShiftMonths);
        var supplierPayments = ShiftSeries(
            source.Select(x => x.SupplierPayments).ToArray(),
            scenario,
            SensitivityDriver.CreditorPaymentShiftMonths);

        var months = new List<CashflowProjectionMonth>(source.Length);
        var runningBank = source[0].OpeningBank;
        decimal runningDebtors = source[0].OpeningDebtors;
        decimal runningCreditors = source[0].OpeningCreditors;

        for (var index = 0; index < source.Length; index++)
        {
            var original = source[index];
            var month = original.MonthNumber;
            var revenueFactor = Factor(scenario, SensitivityDriver.RevenuePercent, month);
            var expenseFactor = Factor(scenario, SensitivityDriver.OperatingExpensesPercent, month);
            var interestFactor = Factor(scenario, SensitivityDriver.LoanInterestPercent, month);
            var assetFactor = Factor(scenario, SensitivityDriver.AssetPurchasesPercent, month);
            var cogsFactor = Factor(scenario, SensitivityDriver.CostOfSalesRelativePercent, month);
            var revenue = original.Revenue * revenueFactor;
            var cogs = original.CostOfGoodsSold * cogsFactor;
            var cogsVat = original.CostOfGoodsSoldVatInclusive * cogsFactor;
            foreach (var adjustment in scenario.Adjustments.Where(x =>
                         x.Driver == SensitivityDriver.CostOfSalesPercentagePoints && x.AppliesTo(month)))
            {
                var rate = original.Revenue == 0m ? 0m : original.CostOfGoodsSold / original.Revenue;
                cogs = revenue * Math.Max(0m, rate + adjustment.Value / 100m);
                cogsVat = original.CostOfGoodsSold == 0m
                    ? cogs
                    : original.CostOfGoodsSoldVatInclusive * (cogs / original.CostOfGoodsSold);
            }

            var badDebt = scenario.Adjustments
                .Where(x => x.Driver == SensitivityDriver.BadDebtPercentagePoints && x.AppliesTo(month))
                .Sum(x => x.Value) / 100m;
            var receipt = salesReceipts[index] * revenueFactor * Math.Clamp(1m - badDebt, 0m, 1m);
            var supplier = supplierPayments[index] * (original.CostOfGoodsSold == 0m ? cogsFactor : cogs / original.CostOfGoodsSold);
            var cashExpenses = original.CashOperatingExpenses * expenseFactor;
            var badDebtExpense = revenue * Math.Max(0m, badDebt);
            var operatingExpenses = original.OperatingExpenses * expenseFactor + badDebtExpense;
            var operatingExpensesVat = original.OperatingExpensesVatInclusive * expenseFactor + badDebtExpense;
            var interest = Math.Max(0m, original.InterestExpense - original.OverdraftInterest) * interestFactor
                + original.OverdraftInterest;
            var assetPurchases = original.AssetPurchases * assetFactor;
            var salesVatInclusive = original.SalesVatInclusive * revenueFactor;

            var accruedSales = salesVatInclusive;
            var baselinePurchases = original.ClosingCreditors + original.SupplierPayments - original.OpeningCreditors;
            var accruedPurchases = baselinePurchases
                * (original.CostOfGoodsSold == 0m ? cogsFactor : cogs / original.CostOfGoodsSold);
            runningDebtors = Math.Max(0m, runningDebtors + accruedSales - receipt - badDebtExpense);
            runningCreditors = Math.Max(0m, runningCreditors + accruedPurchases - supplier);

            var inflows = receipt + original.SundryIncomeReceipts + original.GrantsDonationsReceipts
                + original.AssetDisposalReceipts + original.OtherIncomeReceipts + original.VatRefund;
            var outflows = supplier + cashExpenses + original.VatPayment + original.LoanCashPayment
                + original.OverdraftInterest + assetPurchases;
            var closingBank = runningBank + inflows - outflows;

            months.Add(new CashflowProjectionMonth
            {
                MonthNumber = month,
                Revenue = revenue,
                SundryIncome = original.SundryIncome,
                GrantsDonationsIncome = original.GrantsDonationsIncome,
                ClassifiedAssetDisposalIncome = original.ClassifiedAssetDisposalIncome,
                OtherIncome = original.OtherIncome,
                SalesVatInclusive = salesVatInclusive,
                SundryIncomeVatInclusive = original.SundryIncomeVatInclusive,
                GrantsDonationsVatInclusive = original.GrantsDonationsVatInclusive,
                AssetDisposalIncomeVatInclusive = original.AssetDisposalIncomeVatInclusive,
                OtherIncomeVatInclusive = original.OtherIncomeVatInclusive,
                StockPurchases = original.StockPurchases,
                CostOfGoodsSold = cogs,
                CostOfGoodsSoldVatInclusive = cogsVat,
                OperatingExpenses = operatingExpenses,
                OperatingExpensesVatInclusive = operatingExpensesVat,
                Depreciation = original.Depreciation,
                InterestExpense = interest,
                SalesReceipts = receipt,
                SundryIncomeReceipts = original.SundryIncomeReceipts,
                GrantsDonationsReceipts = original.GrantsDonationsReceipts,
                AssetDisposalReceipts = original.AssetDisposalReceipts,
                OtherIncomeReceipts = original.OtherIncomeReceipts,
                SupplierPayments = supplier,
                CashOperatingExpenses = cashExpenses,
                VatOutput = original.VatOutput,
                VatInput = original.VatInput,
                VatPayable = original.VatPayable,
                ExpectedLoanRepayment = original.ExpectedLoanRepayment,
                ExtraLoanRepayment = original.ExtraLoanRepayment,
                OverdraftInterest = original.OverdraftInterest,
                PrincipalRepaid = original.PrincipalRepaid,
                AssetPurchases = assetPurchases,
                OpeningBank = runningBank,
                ClosingBank = closingBank,
                OpeningDebtors = index == 0 ? original.OpeningDebtors : months[^1].ClosingDebtors,
                ClosingDebtors = runningDebtors,
                OpeningCreditors = index == 0 ? original.OpeningCreditors : months[^1].ClosingCreditors,
                ClosingCreditors = runningCreditors,
                ClosingStock = original.ClosingStock,
                ClosingFixedAssets = original.ClosingFixedAssets + (assetPurchases - original.AssetPurchases),
                ClosingLoanBalance = original.ClosingLoanBalance,
                OtherCurrentAssets = original.OtherCurrentAssets,
                OtherCurrentLiabilities = original.OtherCurrentLiabilities
            });
            runningBank = closingBank;
        }

        var projection = BuildProjection(baseline, months);
        var reserve = Reserve(scenario);
        var metrics = BuildMetrics(projection, reserve);
        return new()
        {
            Scenario = scenario,
            Projection = projection,
            Metrics = metrics,
            Findings = BuildFindings(metrics, reserve),
            Explanations = Explain(scenario, metrics, BuildMetrics(baseline, reserve)),
            CalculationNotes =
            [
                "Calculated in memory from a fresh central cashflow projection; authoritative source records are never mutated.",
                "Revenue, cost, expense, interest and asset changes are deterministic projection overlays. Existing VAT schedules remain unchanged.",
                "Collection and supplier timing shifts move projected cash receipts/payments; amounts outside M1–M12 fall outside this analysis horizon."
            ]
        };
    }

    private static CashflowProjectionResult BuildProjection(
        CashflowProjectionResult baseline,
        IReadOnlyList<CashflowProjectionMonth> months)
    {
        var first = months[0];
        var last = months[^1];
        var revenue = months.Sum(x => x.Revenue);
        var grossProfit = months.Sum(x => x.GrossProfit);
        var operatingExpenses = months.Sum(x => x.OperatingExpenses);
        var depreciation = months.Sum(x => x.Depreciation);
        var interest = months.Sum(x => x.InterestExpense);
        var summary = new CashflowProjectionSummary
        {
            OpeningBank = first.OpeningBank,
            ClosingBank = last.ClosingBank,
            OpeningStock = baseline.Summary.OpeningStock,
            ClosingStock = last.ClosingStock,
            Revenue = revenue,
            SundryIncome = months.Sum(x => x.SundryIncome),
            GrantsDonationsIncome = months.Sum(x => x.GrantsDonationsIncome),
            AssetDisposalIncome = months.Sum(x => x.ClassifiedAssetDisposalIncome),
            OtherIncome = months.Sum(x => x.OtherIncome),
            GrossIncome = months.Sum(x => x.GrossIncome),
            SalesVatInclusive = months.Sum(x => x.SalesVatInclusive),
            SundryIncomeVatInclusive = months.Sum(x => x.SundryIncomeVatInclusive),
            OtherIncomeVatInclusive = months.Sum(x => x.GrantsDonationsVatInclusive + x.AssetDisposalIncomeVatInclusive + x.OtherIncomeVatInclusive),
            CostOfGoodsSoldVatInclusive = months.Sum(x => x.CostOfGoodsSoldVatInclusive),
            GrossProfitVatInclusive = months.Sum(x => x.GrossProfitVatInclusive),
            GrossIncomeVatInclusive = months.Sum(x => x.GrossIncomeVatInclusive),
            OperatingExpensesVatInclusive = months.Sum(x => x.OperatingExpensesVatInclusive),
            EBITDAInclusive = months.Sum(x => x.EBITDAInclusive),
            ProfitBeforeTaxVatInclusive = months.Sum(x => x.ProfitBeforeTaxVatInclusive),
            StockPurchases = months.Sum(x => x.StockPurchases),
            CostOfGoodsSold = months.Sum(x => x.CostOfGoodsSold),
            GrossProfit = grossProfit,
            OperatingExpenses = operatingExpenses,
            EBITDA = months.Sum(x => x.EBITDA),
            Depreciation = depreciation,
            EBIT = months.Sum(x => x.EBIT),
            InterestExpense = interest,
            ProfitBeforeTax = months.Sum(x => x.ProfitBeforeTax),
            VatPayable = months.Sum(x => x.VatPayable),
            TotalInflows = months.Sum(x => x.TotalInflows),
            TotalOutflows = months.Sum(x => x.TotalOutflows),
            NetCashflow = months.Sum(x => x.NetCashflow),
            MinimumClosingBank = months.Min(x => x.ClosingBank),
            ClosingDebtors = last.ClosingDebtors,
            ClosingCreditors = last.ClosingCreditors,
            ClosingFixedAssets = last.ClosingFixedAssets,
            ClosingLoanBalance = last.ClosingLoanBalance,
            CurrentAssets = last.CurrentAssets,
            TotalAssets = last.TotalAssets,
            CurrentLiabilities = last.CurrentLiabilities,
            CurrentRatio = last.CurrentRatio
        };
        var contribution = revenue == 0m ? null : (decimal?)(grossProfit / revenue);
        decimal? breakEven = contribution > 0m
            ? operatingExpenses / contribution.Value
            : null;
        decimal? marginSafety = breakEven.HasValue && revenue != 0m
            ? (revenue - breakEven.Value) / revenue
            : null;
        var viability = new CashflowViabilityMetrics
        {
            ContributionMarginRatio = contribution,
            BreakEvenSales = breakEven,
            MarginOfSafety = marginSafety,
            InterestCover = interest == 0m ? null : (grossProfit - operatingExpenses - depreciation) / interest,
            DebtServiceCoverageRatio = months.Sum(x => x.LoanCashPayment) == 0m ? null : months.Sum(x => x.EBITDA) / months.Sum(x => x.LoanCashPayment),
            CurrentRatio = last.CurrentRatio
        };
        return new()
        {
            AssessmentId = baseline.AssessmentId,
            Months = months,
            Summary = summary,
            Viability = viability,
            Alerts = baseline.Alerts,
            CalculatedAtUtc = DateTime.UtcNow
        };
    }

    private static SensitivityMetrics BuildMetrics(CashflowProjectionResult projection, decimal reserve)
    {
        var minimum = projection.Months.MinBy(x => x.ClosingBank)!;
        return new()
        {
            Revenue = projection.Summary.Revenue,
            GrossProfit = projection.Summary.GrossProfit,
            GrossMarginPercent = projection.Summary.Revenue == 0m ? null : projection.Summary.GrossProfit / projection.Summary.Revenue * 100m,
            EBITDA = projection.Summary.EBITDA,
            ProfitBeforeTax = projection.Summary.ProfitBeforeTax,
            ClosingCash = projection.Summary.ClosingBank,
            MinimumCash = minimum.ClosingBank,
            MinimumCashMonth = minimum.MonthNumber,
            FundingShortfall = Math.Max(0m, reserve - minimum.ClosingBank),
            CurrentRatio = projection.Summary.CurrentRatio,
            InterestCover = projection.Viability.InterestCover,
            BreakEvenSales = projection.Viability.BreakEvenSales,
            MarginOfSafety = projection.Viability.MarginOfSafety
        };
    }

    private static IReadOnlyList<SensitivityFinding> BuildFindings(SensitivityMetrics metrics, decimal reserve)
    {
        var findings = new List<SensitivityFinding>();
        if (metrics.ProfitBeforeTax < 0m)
            findings.Add(new(SensitivityStatus.Red, 1, "Negative projected profit", $"PBT is {metrics.ProfitBeforeTax:N0}.", "Review pricing, gross margin and discretionary expenditure immediately."));
        if (metrics.MinimumCash < 0m)
            findings.Add(new(SensitivityStatus.Red, 1, "Cash shortfall", $"Cash reaches {metrics.MinimumCash:N0} in M{metrics.MinimumCashMonth}.", "Secure working capital before the shortfall month and accelerate collections."));
        else if (metrics.MinimumCash < reserve)
            findings.Add(new(SensitivityStatus.Amber, 2, "Cash reserve breached", $"Cash falls {reserve - metrics.MinimumCash:N0} below the selected reserve.", "Phase spending and preserve a practical contingency buffer."));
        if (metrics.InterestCover is < 1.5m)
            findings.Add(new(metrics.InterestCover < 1m ? SensitivityStatus.Red : SensitivityStatus.Amber, 2, "Weak interest cover", $"Interest cover is {metrics.InterestCover:F2}x.", "Avoid additional debt and discuss repayment flexibility with lenders."));
        if (metrics.CurrentRatio is < 1.2m)
            findings.Add(new(metrics.CurrentRatio < 1m ? SensitivityStatus.Red : SensitivityStatus.Amber, 3, "Weak short-term liquidity", $"Current ratio is {metrics.CurrentRatio:F2}x.", "Tighten debtor collection and negotiate supplier terms."));
        if (findings.Count == 0)
            findings.Add(new(SensitivityStatus.Green, 9, "No key threshold breach", "Profit, cash and supported coverage tests remain above their thresholds.", "Continue monthly monitoring and retain contingency plans."));
        return findings.OrderBy(x => x.Priority).ToArray();
    }

    private static IReadOnlyList<string> DetectFlags(SensitivityMetrics metrics, decimal reserve)
    {
        var flags = new List<string>();
        if (metrics.ProfitBeforeTax < 0m) flags.Add("Negative profit");
        if (metrics.MinimumCash < 0m) flags.Add("Negative cash");
        else if (metrics.MinimumCash < reserve) flags.Add("Cash reserve breach");
        if (metrics.InterestCover is < 1.5m) flags.Add("Weak debt coverage");
        return flags;
    }

    private static SensitivityStatus StatusFor(IReadOnlyCollection<string> flags) =>
        flags.Any(x => x is "Negative profit" or "Negative cash") ? SensitivityStatus.Red
        : flags.Count > 0 ? SensitivityStatus.Amber
        : SensitivityStatus.Green;

    private static decimal[] ShiftSeries(
        decimal[] values,
        SensitivityScenario scenario,
        SensitivityDriver driver)
    {
        var result = new decimal[values.Length];
        for (var source = 0; source < values.Length; source++)
        {
            var shift = (int)Math.Round(scenario.Adjustments
                .Where(x => x.Driver == driver && x.AppliesTo(source + 1))
                .Sum(x => x.Value));
            var destination = source + shift;
            if (destination >= 0 && destination < values.Length)
                result[destination] += values[source];
        }
        return result;
    }

    private static decimal Factor(SensitivityScenario scenario, SensitivityDriver driver, int month) =>
        Math.Max(0m, 1m + scenario.Adjustments
            .Where(x => x.Driver == driver && x.AppliesTo(month))
            .Sum(x => x.Value) / 100m);

    private static decimal Reserve(SensitivityScenario scenario) =>
        Math.Max(0m, scenario.Adjustments
            .Where(x => x.Driver == SensitivityDriver.MinimumCashReserve)
            .Select(x => x.Value)
            .LastOrDefault());

    private static IReadOnlyList<string> Explain(
        SensitivityScenario scenario,
        SensitivityMetrics result,
        SensitivityMetrics baseline)
    {
        var explanations = scenario.Adjustments
            .Where(x => x.Value != 0m)
            .OrderByDescending(x => Math.Abs(x.Value))
            .Take(3)
            .Select(x => $"{DriverLabel(x.Driver)} changes by {x.Value:+0.##;-0.##;0} from M{x.EffectiveStartMonth}"
                + (x.DurationMonths.HasValue ? $" for {x.DurationMonths} month(s)." : " onward."))
            .ToList();
        explanations.Add($"Compared with baseline, PBT changes by {result.ProfitBeforeTax - baseline.ProfitBeforeTax:N0} and closing cash by {result.ClosingCash - baseline.ClosingCash:N0}.");
        return explanations;
    }

    private static decimal? FindThreshold(
        CashflowProjectionResult baseline,
        SensitivityDriver driver,
        decimal increment,
        decimal limit,
        decimal reserve)
    {
        for (var value = increment; Math.Abs(value) <= Math.Abs(limit); value += increment)
        {
            var scenario = new SensitivityScenario
            {
                Name = "Threshold test",
                Adjustments = [new(driver, value)]
            };
            var metrics = BuildMetrics(Calculate(baseline, scenario).Projection, reserve);
            if (metrics.ProfitBeforeTax < 0m || metrics.MinimumCash < reserve)
                return Math.Abs(value);
        }
        return null;
    }

    private static SensitivityThreshold Threshold(
        string name,
        decimal? value,
        string unit,
        string interpretation,
        SensitivityStatus status = SensitivityStatus.Amber) =>
        new(name, value, unit, interpretation, status);

    public static string DriverLabel(SensitivityDriver driver) => driver switch
    {
        SensitivityDriver.RevenuePercent => "Sales / revenue",
        SensitivityDriver.CostOfSalesRelativePercent => "Cost of sales (relative)",
        SensitivityDriver.CostOfSalesPercentagePoints => "Cost of sales (percentage points)",
        SensitivityDriver.OperatingExpensesPercent => "Operating expenses",
        SensitivityDriver.BadDebtPercentagePoints => "Bad debt",
        SensitivityDriver.DebtorCollectionShiftMonths => "Debtor collection timing",
        SensitivityDriver.CreditorPaymentShiftMonths => "Creditor payment timing",
        SensitivityDriver.LoanInterestPercent => "Loan interest",
        SensitivityDriver.AssetPurchasesPercent => "Asset purchases",
        SensitivityDriver.MinimumCashReserve => "Minimum cash reserve",
        _ => driver.ToString()
    };
}
