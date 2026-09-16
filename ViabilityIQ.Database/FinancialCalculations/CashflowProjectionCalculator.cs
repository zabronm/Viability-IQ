using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed record CashflowCalculationInput(
    Assessment Assessment,
    IReadOnlyList<AssessmentSalesCategory> Categories,
    IReadOnlyList<AssessmentSales> Sales,
    IReadOnlyList<IncomeType> IncomeTypes,
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
    private const int ProjectionMonths = 12;

    public static CashflowProjectionResult Calculate(CashflowCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var warnings = new List<CashflowProjectionAlert>();
        var months = new List<CashflowProjectionMonth>(ProjectionMonths);
        var classifier = new IncomeTypeClassifier(input.IncomeTypes);
        var categories = input.Categories.ToDictionary(x => x.AssessmentSalesCategoryId);
        var classifiedSales = input.Sales
            .Select(sale =>
            {
                categories.TryGetValue(sale.ProductCategoryId, out var category);
                return new ClassifiedSale(sale, category, classifier.Classify(sale, category));
            })
            .ToList();

        AddClassificationWarnings(classifiedSales, classifier, warnings);

        var salesCategories = input.Categories
            .Where(x => classifier.Classify(x) == IncomeSourceKind.Sales)
            .ToList();
        var openingStock = salesCategories.Sum(x => x.OpeningStock);
        var openingLoan = input.Loans.Sum(x => x.LoanBalanceAtAssessmentDate);
        var debtorMonths = BuildSalesDebtorProjection(
            classifiedSales,
            salesCategories.Sum(x => x.OpeningDebtorsAmount),
            input.Accounts.Profile);
        var hasClassifiedAssetDisposal = classifiedSales.Any(x =>
            x.Kind == IncomeSourceKind.AssetDisposal);

        var runningBank = input.Assessment.OpeningBalance_Bank;
        var runningStock = openingStock;
        var runningLoan = openingLoan;

        if (input.Assessment.blDebtorsCreditors == 1 && !input.Accounts.Profile.IsConfigured)
        {
            warnings.Add(new(
                "accounts-profile",
                CashflowAlertSeverity.Warning,
                "Accounts profile incomplete",
                "Sales debtor collection timing is not fully configured."));
        }

        if (!hasClassifiedAssetDisposal
            && input.AssetMovements.Any(x =>
                Enumerable.Range(1, ProjectionMonths).Any(month =>
                    string.Equals(
                        x.GetMovementType(month),
                        "Disposal",
                        StringComparison.OrdinalIgnoreCase)
                    && x.GetMovementValue(month) != 0m)))
        {
            warnings.Add(new(
                "asset-disposal-fallback",
                CashflowAlertSeverity.Warning,
                "Asset disposal income inferred",
                "No Asset Disposal income rows were found. Asset movement disposal values are used as proceeds for backward compatibility."));
        }

        for (var month = 1; month <= ProjectionMonths; month++)
        {
            var index = month - 1;
            var salesNet = IncomeAmount(classifiedSales, IncomeSourceKind.Sales, index, false);
            var salesGross = IncomeAmount(classifiedSales, IncomeSourceKind.Sales, index, true);
            var sundryNet = IncomeAmount(classifiedSales, IncomeSourceKind.SundryIncome, index, false);
            var sundryGross = IncomeAmount(classifiedSales, IncomeSourceKind.SundryIncome, index, true);
            var grantsNet = IncomeAmount(classifiedSales, IncomeSourceKind.GrantsDonations, index, false);
            var grantsGross = IncomeAmount(classifiedSales, IncomeSourceKind.GrantsDonations, index, true);
            var classifiedDisposalNet =
                IncomeAmount(classifiedSales, IncomeSourceKind.AssetDisposal, index, false);
            var classifiedDisposalGross =
                IncomeAmount(classifiedSales, IncomeSourceKind.AssetDisposal, index, true);
            var otherNet = IncomeAmount(classifiedSales, IncomeSourceKind.OtherIncome, index, false);
            var otherGross = IncomeAmount(classifiedSales, IncomeSourceKind.OtherIncome, index, true);

            var movementDisposal = hasClassifiedAssetDisposal
                ? 0m
                : MovementTotal(input.AssetMovements, month, "Disposal");
            var assetDisposalNet = classifiedDisposalNet + movementDisposal;
            var assetDisposalGross = classifiedDisposalGross + movementDisposal;
            var stockPurchases = input.Stock.Sum(x => Value(x.MonthlyValues, index));
            var cogs = CalculateCogs(input, classifiedSales, index, warnings, false);
            var cogsInclusive = CalculateCogs(input, classifiedSales, index, warnings, true);
            var operatingExpenses = input.Expenses.Sum(x => Value(x.MonthlyValues, index))
                                    + input.Assessment.MonthlyDirectorWagesAmountTotal;
            var operatingExpensesInclusive = input.Expenses
                .Sum(x => ExpenseAmount(x, index, true))
                + input.Assessment.MonthlyDirectorWagesAmountTotal;
            var cashOperatingExpenses = input.Expenses
                .Where(x => x.blSendToCashBook)
                .Sum(x => ExpenseAmount(x, index, true))
                + input.Assessment.MonthlyDirectorWagesAmountTotal;

            var accountsMonth = input.Accounts.Months.FirstOrDefault(x => x.MonthNumber == month);
            var debtorMonth = debtorMonths[index];
            var salesReceipts = input.Assessment.blDebtorsCreditors == 1
                ? debtorMonth.Collections
                : salesGross;
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
                ? Math.Abs(runningBank)
                  * Math.Max(input.Assessment.InterestOnOverDraft, 0m)
                  / 100m
                  / 12m
                : 0m;
            var additions = MovementTotal(input.AssetMovements, month, "Addition");
            var depreciation = input.Depreciation.TryGetValue(month, out var value) ? value : 0m;
            var closingFixedAssets = input.AssetMovements
                .Sum(x => Math.Max(0m, x.GetNetBookValue(month) ?? 0m));

            runningStock = Math.Max(0m, runningStock + stockPurchases - cogs);
            runningLoan = Math.Max(0m, runningLoan - principal);
            var inflows = salesReceipts
                          + sundryGross
                          + grantsGross
                          + assetDisposalGross
                          + otherGross
                          + Math.Max(0m, -vatPayable);
            var outflows = supplierPayments
                           + cashOperatingExpenses
                           + Math.Max(0m, vatPayable)
                           + expected
                           + extra
                           + overdraftInterest
                           + additions;
            var closingBank = runningBank + inflows - outflows;

            months.Add(new CashflowProjectionMonth
            {
                MonthNumber = month,
                Revenue = salesNet,
                SundryIncome = sundryNet,
                GrantsDonationsIncome = grantsNet,
                ClassifiedAssetDisposalIncome = assetDisposalNet,
                OtherIncome = otherNet,
                SalesVatInclusive = salesGross,
                SundryIncomeVatInclusive = sundryGross,
                GrantsDonationsVatInclusive = grantsGross,
                AssetDisposalIncomeVatInclusive = assetDisposalGross,
                OtherIncomeVatInclusive = otherGross,
                StockPurchases = stockPurchases,
                CostOfGoodsSold = cogs,
                CostOfGoodsSoldVatInclusive = cogsInclusive,
                OperatingExpenses = operatingExpenses,
                OperatingExpensesVatInclusive = operatingExpensesInclusive,
                Depreciation = depreciation,
                InterestExpense = loanInterest + overdraftInterest,
                SalesReceipts = salesReceipts,
                SundryIncomeReceipts = sundryGross,
                GrantsDonationsReceipts = grantsGross,
                AssetDisposalReceipts = assetDisposalGross,
                OtherIncomeReceipts = otherGross,
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
                OpeningBank = runningBank,
                ClosingBank = closingBank,
                OpeningDebtors = debtorMonth.OpeningBalance,
                ClosingDebtors = debtorMonth.ClosingBalance,
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
        IReadOnlyList<ClassifiedSale> classifiedSales,
        int index,
        ICollection<CashflowProjectionAlert> warnings,
        bool vatInclusive)
    {
        decimal result = 0m;
        foreach (var category in input.Categories)
        {
            var categorySales = classifiedSales
                .Where(x =>
                    x.Sale.ProductCategoryId == category.AssessmentSalesCategoryId
                    && x.Kind == IncomeSourceKind.Sales)
                .Sum(x => SaleAmount(x.Sale, index, false));
            if (categorySales == 0m)
            {
                continue;
            }

            if (category.MarkupPercentage > 0m)
            {
                var netCogs = categorySales / (1m + category.MarkupPercentage / 100m);
                result += vatInclusive
                    ? netCogs * StockVatFactor(input.Stock, category.AssessmentSalesCategoryId)
                    : netCogs;
            }
            else
            {
                result += input.Stock
                    .Where(x =>
                        x.AssessmentSalesCategoryId == category.AssessmentSalesCategoryId)
                    .Sum(x => vatInclusive && x.blIncludeVAT
                        ? Gross(Value(x.MonthlyValues, index), x.TotalNoVAT, x.TotalWithVAT)
                        : Value(x.MonthlyValues, index));
                var code = $"markup-{category.AssessmentSalesCategoryId}";
                if (warnings.All(x => x.Code != code))
                {
                    warnings.Add(new(
                        code,
                        CashflowAlertSeverity.Warning,
                        "Missing category markup",
                        $"{category.AssessmentSalesCategoryName ?? "Unnamed category"} uses stock purchases as COGS fallback."));
                }
            }
        }

        return result;
    }

    private static decimal StockVatFactor(
        IEnumerable<AssessmentStock> stock,
        long categoryId)
    {
        var rows = stock
            .Where(x =>
                x.AssessmentSalesCategoryId == categoryId
                && x.blIncludeVAT
                && x.TotalNoVAT > 0m
                && x.TotalWithVAT >= x.TotalNoVAT)
            .ToList();
        var totalNet = rows.Sum(x => x.TotalNoVAT);
        return totalNet == 0m ? 1m : rows.Sum(x => x.TotalWithVAT) / totalNet;
    }

    private static IReadOnlyList<DebtorMonth> BuildSalesDebtorProjection(
        IReadOnlyList<ClassifiedSale> classifiedSales,
        decimal openingDebtors,
        AccountsProfileSummary profile)
    {
        var sales = Enumerable.Range(0, ProjectionMonths)
            .Select(index => classifiedSales
                .Where(x => x.Kind == IncomeSourceKind.Sales)
                .Sum(x => SaleAmount(x.Sale, index, profile.IncludeVat)))
            .ToArray();
        var buckets = Normalize(profile.DebtorBuckets);
        var collections = BuildSchedule(sales, buckets, profile.BadDebtPercentage);
        AddOpeningBalanceSchedule(collections, openingDebtors, buckets);

        var result = new List<DebtorMonth>(ProjectionMonths);
        var opening = openingDebtors;
        for (var index = 0; index < ProjectionMonths; index++)
        {
            var badDebt = sales[index]
                          * Math.Clamp(profile.BadDebtPercentage, 0m, 100m)
                          / 100m;
            var closing = Math.Max(0m, opening + sales[index] - collections[index] - badDebt);
            result.Add(new DebtorMonth(opening, collections[index], closing));
            opening = closing;
        }

        return result;
    }

    private static decimal[] BuildSchedule(
        IReadOnlyList<decimal> source,
        IReadOnlyList<ProfileBucket> buckets,
        decimal deductionPercentage)
    {
        var schedule = new decimal[ProjectionMonths];
        var distributableFactor =
            1m - Math.Clamp(deductionPercentage, 0m, 100m) / 100m;
        for (var sourceMonth = 0; sourceMonth < ProjectionMonths; sourceMonth++)
        {
            foreach (var bucket in buckets)
            {
                var destination = sourceMonth + bucket.DelayMonths;
                if (destination < ProjectionMonths)
                {
                    schedule[destination] +=
                        source[sourceMonth] * distributableFactor * bucket.Percentage / 100m;
                }
            }
        }

        return schedule;
    }

    private static void AddOpeningBalanceSchedule(
        decimal[] schedule,
        decimal openingBalance,
        IReadOnlyList<ProfileBucket> buckets)
    {
        foreach (var bucket in buckets.Where(x => x.DelayMonths < ProjectionMonths))
        {
            schedule[bucket.DelayMonths] += openingBalance * bucket.Percentage / 100m;
        }
    }

    private static IReadOnlyList<ProfileBucket> Normalize(IReadOnlyList<ProfileBucket> buckets)
    {
        var total = buckets.Sum(x => Math.Max(x.Percentage, 0m));
        return total <= 0m
            ? buckets.Select(x => x with { Percentage = 0m }).ToList()
            : buckets.Select(x =>
                x with { Percentage = Math.Max(x.Percentage, 0m) * 100m / total }).ToList();
    }

    private static void AddClassificationWarnings(
        IEnumerable<ClassifiedSale> sales,
        IncomeTypeClassifier classifier,
        ICollection<CashflowProjectionAlert> warnings)
    {
        var unknown = sales
            .Where(x =>
                !classifier.IsKnown(x.Sale.IncomeTypeId)
                && (x.Category is null || !classifier.IsKnown(x.Category.IncomeTypeId)))
            .ToList();
        if (unknown.Count > 0)
        {
            warnings.Add(new(
                "unknown-income-type",
                CashflowAlertSeverity.Warning,
                "Unclassified income",
                $"{unknown.Count} income row(s) have no active tblIncomeType mapping and are included under Other Income."));
        }
    }

    private static decimal IncomeAmount(
        IEnumerable<ClassifiedSale> rows,
        IncomeSourceKind kind,
        int index,
        bool vatInclusive) =>
        rows.Where(x => x.Kind == kind)
            .Sum(x => SaleAmount(x.Sale, index, vatInclusive));

    private static decimal SaleAmount(AssessmentSales sale, int index, bool vatInclusive)
    {
        var net = Value(sale.MonthlyValues, index);
        return vatInclusive && sale.IncludeVAT != 0m
            ? Gross(net, sale.TotalNoVAT, sale.TotalWithVAT)
            : net;
    }

    private static decimal ExpenseAmount(
        AssessmentExpenses expense,
        int index,
        bool vatInclusive)
    {
        var net = Value(expense.MonthlyValues, index);
        return vatInclusive && expense.TotalWithVAT > expense.TotalNoVAT
            ? Gross(net, expense.TotalNoVAT, expense.TotalWithVAT)
            : net;
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
            SundryIncome = months.Sum(x => x.SundryIncome),
            GrantsDonationsIncome = months.Sum(x => x.GrantsDonationsIncome),
            AssetDisposalIncome = months.Sum(x => x.ClassifiedAssetDisposalIncome),
            OtherIncome = months.Sum(x => x.OtherIncome),
            GrossIncome = months.Sum(x => x.GrossIncome),
            SalesVatInclusive = months.Sum(x => x.SalesVatInclusive),
            SundryIncomeVatInclusive = months.Sum(x => x.SundryIncomeVatInclusive),
            OtherIncomeVatInclusive = months.Sum(x =>
                x.GrantsDonationsVatInclusive
                + x.AssetDisposalIncomeVatInclusive
                + x.OtherIncomeVatInclusive),
            CostOfGoodsSoldVatInclusive = months.Sum(x => x.CostOfGoodsSoldVatInclusive),
            GrossProfitVatInclusive = months.Sum(x => x.GrossProfitVatInclusive),
            GrossIncomeVatInclusive = months.Sum(x => x.GrossIncomeVatInclusive),
            OperatingExpensesVatInclusive = months.Sum(x => x.OperatingExpensesVatInclusive),
            EBITDAInclusive = months.Sum(x => x.EBITDAInclusive),
            ProfitBeforeTaxVatInclusive = months.Sum(x => x.ProfitBeforeTaxVatInclusive),
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
            (x.ProfitBeforeTax - averageProfit) * (x.ProfitBeforeTax - averageProfit))
            / months.Count;
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

    private sealed record ClassifiedSale(
        AssessmentSales Sale,
        AssessmentSalesCategory? Category,
        IncomeSourceKind Kind);

    private sealed record DebtorMonth(
        decimal OpeningBalance,
        decimal Collections,
        decimal ClosingBalance);
}
