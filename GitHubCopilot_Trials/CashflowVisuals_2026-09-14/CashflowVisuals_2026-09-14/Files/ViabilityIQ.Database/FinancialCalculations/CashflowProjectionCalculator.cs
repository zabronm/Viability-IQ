using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed record CashflowCalculationInput(
    Assessment Assessment,
    IReadOnlyList<AssessmentSalesCategory> Categories,
    IReadOnlyList<AssessmentSales> Sales,
    IReadOnlyList<AssessmentStock> Stock,
    IReadOnlyList<AssessmentExpenses> Expenses,
    IReadOnlyList<MonthlyVatProjection> VatProjection,
    IReadOnlyList<AssessmentLoan> Loans,
    IReadOnlyList<AssessmentLoanRepayment> LoanRepayments,
    IReadOnlyList<AssessmentAssetMovement> AssetMovements,
    IReadOnlyDictionary<int, decimal> Depreciation,
    AccountsProjectionResult Accounts);

public static class CashflowProjectionCalculator
{
    public static CashflowProjectionResult Calculate(CashflowCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var warnings = new List<CashflowProjectionAlert>();
        var months = new List<CashflowProjectionMonth>(12);
        var openingStock = input.Categories.Sum(x => x.OpeningStock);
        var openingLoan = input.Loans.Sum(x => x.LoanBalanceAtAssessmentDate);
        var runningBank = input.Assessment.OpeningBalance_Bank;
        var runningStock = openingStock;
        var runningLoan = openingLoan;

        if (input.Assessment.blDebtorsCreditors == 1 && !input.Accounts.Profile.IsConfigured)
        {
            warnings.Add(new(
                "accounts-profile",
                CashflowAlertSeverity.Warning,
                "Accounts profile incomplete",
                "Debtor/creditor collection timing is not fully configured."));
        }

        for (var month = 1; month <= 12; month++)
        {
            var index = month - 1;
            var revenue = input.Sales.Sum(x => Value(x.MonthlyValues, index));
            var stockPurchases = input.Stock.Sum(x => Value(x.MonthlyValues, index));
            var cogs = CalculateCogs(input, index, warnings);
            var operatingExpenses = input.Expenses.Sum(x => Value(x.MonthlyValues, index))
                                    + input.Assessment.MonthlyDirectorWagesAmountTotal;
            var cashOperatingExpenses = input.Expenses
                .Where(x => x.blSendToCashBook)
                .Sum(x => Gross(Value(x.MonthlyValues, index), x.TotalNoVAT, x.TotalWithVAT))
                + input.Assessment.MonthlyDirectorWagesAmountTotal;

            var accountsMonth = input.Accounts.Months.FirstOrDefault(x => x.MonthNumber == month);
            var salesReceipts = input.Assessment.blDebtorsCreditors == 1
                ? accountsMonth?.DebtorCollections ?? 0m
                : input.Sales.Sum(x => x.IncludeVAT != 0m
                    ? Gross(Value(x.MonthlyValues, index), x.TotalNoVAT, x.TotalWithVAT)
                    : Value(x.MonthlyValues, index));
            var supplierPayments = input.Assessment.blDebtorsCreditors == 1
                ? accountsMonth?.CreditorPayments ?? 0m
                : input.Stock.Sum(x => x.blIncludeVAT
                    ? Gross(Value(x.MonthlyValues, index), x.TotalNoVAT, x.TotalWithVAT)
                    : Value(x.MonthlyValues, index));

            var vat = input.VatProjection.FirstOrDefault(x => x.Period == month);
            var vatPayable = vat?.VatPayable ?? 0m;
            var expected = LoanMetric(input.LoanRepayments, 1, index);
            var loanInterest = LoanMetric(input.LoanRepayments, 2, index);
            var extra = LoanMetric(input.LoanRepayments, 3, index);
            var principal = Math.Max(0m, expected - loanInterest) + extra;
            var overdraftInterest = runningBank < 0m
                ? Math.Abs(runningBank) * Math.Max(input.Assessment.InterestOnOverDraft, 0m) / 100m / 12m
                : 0m;
            var additions = MovementTotal(input.AssetMovements, month, "Addition");
            var disposals = MovementTotal(input.AssetMovements, month, "Disposal");
            var depreciation = input.Depreciation.TryGetValue(month, out var value) ? value : 0m;
            var closingFixedAssets = input.AssetMovements
                .Sum(x => Math.Max(0m, x.GetNetBookValue(month) ?? 0m));

            runningStock = Math.Max(0m, runningStock + stockPurchases - cogs);
            runningLoan = Math.Max(0m, runningLoan - principal);
            var inflows = salesReceipts + disposals + Math.Max(0m, -vatPayable);
            var outflows = supplierPayments + cashOperatingExpenses + Math.Max(0m, vatPayable)
                           + expected + extra + overdraftInterest + additions;
            var closingBank = runningBank + inflows - outflows;

            months.Add(new CashflowProjectionMonth
            {
                MonthNumber = month,
                Revenue = revenue,
                StockPurchases = stockPurchases,
                CostOfGoodsSold = cogs,
                OperatingExpenses = operatingExpenses,
                Depreciation = depreciation,
                InterestExpense = loanInterest + overdraftInterest,
                SalesReceipts = salesReceipts,
                SupplierPayments = supplierPayments,
                CashOperatingExpenses = cashOperatingExpenses,
                VatOutput = (vat?.OutputVat ?? 0m) + (vat?.AdjustmentOutputVat ?? 0m),
                VatInput = vat?.InputVat ?? 0m,
                VatPayable = vatPayable,
                ExpectedLoanRepayment = expected,
                ExtraLoanRepayment = extra,
                OverdraftInterest = overdraftInterest,
                PrincipalRepaid = principal,
                AssetPurchases = additions,
                AssetDisposalProceeds = disposals,
                OpeningBank = runningBank,
                ClosingBank = closingBank,
                OpeningDebtors = accountsMonth?.OpeningDebtors ?? 0m,
                ClosingDebtors = accountsMonth?.ClosingDebtors ?? 0m,
                OpeningCreditors = accountsMonth?.OpeningCreditors ?? 0m,
                ClosingCreditors = accountsMonth?.ClosingCreditors ?? 0m,
                ClosingStock = runningStock,
                ClosingFixedAssets = closingFixedAssets,
                ClosingLoanBalance = runningLoan
            });
            runningBank = closingBank;
        }

        var summary = BuildSummary(input.Assessment.OpeningBalance_Bank, openingStock, months);
        var viability = BuildViability(months, summary);
        warnings.AddRange(BuildAlerts(months, summary, viability));
        if (warnings.Count == 0)
        {
            warnings.Add(new(
                "healthy",
                CashflowAlertSeverity.Healthy,
                "Projection healthy",
                "No configured cashflow, liquidity, profitability, or data-quality thresholds were breached."));
        }

        return new CashflowProjectionResult
        {
            AssessmentId = input.Assessment.AssessmentId,
            Months = months,
            Summary = summary,
            Viability = viability,
            Alerts = warnings
        };
    }

    private static decimal CalculateCogs(
        CashflowCalculationInput input,
        int index,
        ICollection<CashflowProjectionAlert> warnings)
    {
        decimal result = 0m;
        foreach (var category in input.Categories)
        {
            var categorySales = input.Sales
                .Where(x => x.ProductCategoryId == category.AssessmentSalesCategoryId)
                .Sum(x => Value(x.MonthlyValues, index));
            if (category.MarkupPercentage > 0m)
            {
                result += categorySales / (1m + category.MarkupPercentage / 100m);
            }
            else
            {
                result += input.Stock
                    .Where(x => x.AssessmentSalesCategoryId == category.AssessmentSalesCategoryId)
                    .Sum(x => Value(x.MonthlyValues, index));
                var code = $"markup-{category.AssessmentSalesCategoryId}";
                if (warnings.All(x => x.Code != code))
                {
                    warnings.Add(new(
                        code,
                        CashflowAlertSeverity.Warning,
                        "Missing category markup",
                        $"{category.AssessmentSalesCategoryName ?? "Unnamed category"} uses net stock purchases as COGS fallback."));
                }
            }
        }

        return result;
    }

    private static CashflowProjectionSummary BuildSummary(
        decimal openingBank,
        decimal openingStock,
        IReadOnlyList<CashflowProjectionMonth> months)
    {
        var last = months[^1];
        return new CashflowProjectionSummary
        {
            OpeningBank = openingBank,
            ClosingBank = last.ClosingBank,
            OpeningStock = openingStock,
            ClosingStock = last.ClosingStock,
            Revenue = months.Sum(x => x.Revenue),
            StockPurchases = months.Sum(x => x.StockPurchases),
            CostOfGoodsSold = months.Sum(x => x.CostOfGoodsSold),
            GrossProfit = months.Sum(x => x.GrossProfit),
            OperatingExpenses = months.Sum(x => x.OperatingExpenses),
            EBITDA = months.Sum(x => x.EBITDA),
            Depreciation = months.Sum(x => x.Depreciation),
            EBIT = months.Sum(x => x.EBIT),
            InterestExpense = months.Sum(x => x.InterestExpense),
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
    }

    private static CashflowViabilityMetrics BuildViability(
        IReadOnlyList<CashflowProjectionMonth> months,
        CashflowProjectionSummary summary)
    {
        var contribution = summary.Revenue == 0m
            ? null
            : (decimal?)(summary.GrossProfit / summary.Revenue);
        decimal? breakEven = contribution > 0m
            ? summary.OperatingExpenses / contribution.Value
            : null;
        var safety = summary.Revenue == 0m || breakEven is null
            ? null
            : (decimal?)((summary.Revenue - breakEven.Value) / summary.Revenue);
        var interestCover = summary.InterestExpense == 0m
            ? null
            : (decimal?)(summary.EBIT / summary.InterestExpense);
        var debtPayments = months.Sum(x => x.LoanCashPayment);
        var dscr = debtPayments == 0m ? null : (decimal?)(summary.EBITDA / debtPayments);
        var monthlyRate = 0.10m / 12m;
        var npv = months.Select((x, index) =>
            x.NetCashflow / (decimal)Math.Pow((double)(1m + monthlyRate), index + 1)).Sum();
        var averageProfit = months.Average(x => x.ProfitBeforeTax);
        var variance = months.Sum(x =>
            (x.ProfitBeforeTax - averageProfit) * (x.ProfitBeforeTax - averageProfit)) / months.Count;
        var volatility = averageProfit == 0m
            ? null
            : (decimal?)(decimal)Math.Sqrt((double)variance) / Math.Abs(averageProfit);
        var assetMonths = months.Where(x => x.TotalAssets > 0m).ToList();
        var averageAssets = assetMonths.Count == 0 ? 0m : assetMonths.Average(x => x.TotalAssets);
        var assetTurnover = averageAssets == 0m ? null : (decimal?)(summary.Revenue / averageAssets);
        var returnOnAssets = averageAssets == 0m
            ? null
            : (decimal?)(summary.ProfitBeforeTax / averageAssets);
        var healthScore = CalculateHealthScore(
            safety,
            interestCover,
            summary.EBIT,
            summary.CurrentRatio,
            summary.NetCashflow,
            returnOnAssets);

        return new CashflowViabilityMetrics
        {
            ContributionMarginRatio = contribution,
            BreakEvenSales = breakEven,
            MarginOfSafety = safety,
            InterestCover = interestCover,
            DebtServiceCoverageRatio = dscr,
            NetPresentValue = npv,
            ProfitVolatility = volatility,
            CurrentRatio = summary.CurrentRatio,
            PaybackMonths = null,
            AssetTurnover = assetTurnover,
            ReturnOnAssets = returnOnAssets,
            HealthScore = healthScore
        };
    }

    private static decimal CalculateHealthScore(
        decimal? marginOfSafety,
        decimal? interestCover,
        decimal ebit,
        decimal? currentRatio,
        decimal netCashflow,
        decimal? returnOnAssets)
    {
        var safetyScore = ClampRatio(marginOfSafety, 0.30m) * 30m;
        var interestScore = interestCover.HasValue
            ? ClampRatio(interestCover, 3m) * 25m
            : ebit > 0m ? 25m : 0m;
        var liquidityScore = ClampRatio(currentRatio, 1.5m) * 20m;
        var cashflowScore = netCashflow > 0m ? 15m : 0m;
        var returnScore = ClampRatio(returnOnAssets, 0.15m) * 10m;
        return Math.Round(
            Math.Clamp(
                safetyScore + interestScore + liquidityScore + cashflowScore + returnScore,
                0m,
                100m),
            1);
    }

    private static decimal ClampRatio(decimal? value, decimal target) =>
        value.HasValue && target > 0m
            ? Math.Clamp(value.Value / target, 0m, 1m)
            : 0m;

    private static IEnumerable<CashflowProjectionAlert> BuildAlerts(
        IReadOnlyList<CashflowProjectionMonth> months,
        CashflowProjectionSummary summary,
        CashflowViabilityMetrics viability)
    {
        if (summary.MinimumClosingBank < 0m)
        {
            yield return new(
                "negative-bank",
                CashflowAlertSeverity.Critical,
                "Negative closing bank",
                $"The minimum projected closing bank is {summary.MinimumClosingBank:N2}.");
        }

        var negativeMonths = months.Count(x => x.NetCashflow < 0m);
        if (negativeMonths > 0)
        {
            yield return new(
                "negative-cashflow",
                CashflowAlertSeverity.Warning,
                "Negative monthly cashflow",
                $"{negativeMonths} month(s) have negative net cashflow.");
        }

        if (summary.VatPayable > 0m)
        {
            yield return new(
                "vat-liability",
                CashflowAlertSeverity.Warning,
                "VAT liability",
                $"Net VAT cash payments over the projection are {summary.VatPayable:N2}.");
        }

        if (summary.ClosingDebtors > summary.Revenue / 4m && summary.ClosingDebtors > 0m)
        {
            yield return new(
                "high-debtors",
                CashflowAlertSeverity.Warning,
                "High closing debtors",
                $"Closing debtors are {summary.ClosingDebtors:N2}.");
        }

        if (summary.ClosingCreditors > summary.CostOfGoodsSold / 4m
            && summary.ClosingCreditors > 0m)
        {
            yield return new(
                "high-creditors",
                CashflowAlertSeverity.Warning,
                "High closing creditors",
                $"Closing creditors are {summary.ClosingCreditors:N2}.");
        }

        if (viability.CurrentRatio is < 1m)
        {
            yield return new(
                "current-ratio",
                CashflowAlertSeverity.Critical,
                "Weak current ratio",
                $"Projected current ratio is {viability.CurrentRatio:F2}.");
        }

        if (viability.InterestCover is < 1.5m)
        {
            yield return new(
                "interest-cover",
                CashflowAlertSeverity.Warning,
                "Insufficient interest cover",
                $"Projected interest cover is {viability.InterestCover:F2}x.");
        }

        if (viability.DebtServiceCoverageRatio is < 1.2m)
        {
            yield return new(
                "dscr",
                CashflowAlertSeverity.Warning,
                "Insufficient debt service cover",
                $"Projected DSCR is {viability.DebtServiceCoverageRatio:F2}x.");
        }
    }

    private static decimal LoanMetric(
        IEnumerable<AssessmentLoanRepayment> rows,
        int metric,
        int index) =>
        rows.Where(x => x.MetricTypeId == metric).Sum(x => Value(x.MonthlyValues, index));

    private static decimal MovementTotal(
        IEnumerable<AssessmentAssetMovement> rows,
        int month,
        string movementType) =>
        rows.Where(x =>
                string.Equals(
                    x.GetMovementType(month),
                    movementType,
                    StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.GetMovementValue(month));

    private static decimal Gross(decimal net, decimal totalNet, decimal totalGross) =>
        totalNet == 0m ? net : net * totalGross / totalNet;

    private static decimal Value(decimal[]? values, int index) =>
        values is not null && index >= 0 && index < values.Length ? values[index] : 0m;
}
