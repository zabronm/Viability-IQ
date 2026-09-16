using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class AccountsProjectionService : IAccountsProjectionService
{
    private const int ProjectionMonths = 12;

    private readonly IGenericDataRepository<AssessmentSalesCategory> _categoryRepository;
    private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
    private readonly IGenericDataRepository<AssessmentStock> _stockRepository;
    private readonly IGenericDataRepository<DebtorsCreditorsProfile> _profileRepository;
    private readonly ILogger<AccountsProjectionService> _logger;

    public AccountsProjectionService(
        IGenericDataRepository<AssessmentSalesCategory> categoryRepository,
        IGenericDataRepository<AssessmentSales> salesRepository,
        IGenericDataRepository<AssessmentStock> stockRepository,
        IGenericDataRepository<DebtorsCreditorsProfile> profileRepository,
        ILogger<AccountsProjectionService> logger)
    {
        _categoryRepository = categoryRepository;
        _salesRepository = salesRepository;
        _stockRepository = stockRepository;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<AccountsProjectionResult> CalculateAsync(long assessmentId)
    {
        if (assessmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assessmentId));
        }

        var categories = (await _categoryRepository.GetAllAsync(x =>
            x.AssessmentId == assessmentId && x.Active)).ToList();
        var sales = (await _salesRepository.GetAllAsync(x =>
            x.AssessmentId == assessmentId && x.Active)).ToList();
        var stock = (await _stockRepository.GetAllAsync(x =>
            x.AssessmentId == assessmentId && x.Active)).ToList();
        var profile = (await _profileRepository.GetAllAsync(x =>
            x.AssessmentId == assessmentId))
            .OrderByDescending(x => x.Active)
            .ThenByDescending(x => x.ModifiedDate)
            .ThenByDescending(x => x.CreatedDate)
            .FirstOrDefault();

        var alerts = new List<AccountsProjectionAlert>();
        var profileSummary = BuildProfile(profile, alerts);
        var debtorBuckets = Normalize(profileSummary.DebtorBuckets);
        var creditorBuckets = Normalize(profileSummary.CreditorBuckets);

        var salesByMonth = SumMonthly(
            sales,
            x => x.MonthlyValues,
            x => VatAdjustedMonthlyValues(x.MonthlyValues, x.TotalNoVAT, x.TotalWithVAT, profileSummary.IncludeVat));
        var purchasesByMonth = SumMonthly(
            stock,
            x => x.MonthlyValues,
            x => VatAdjustedMonthlyValues(x.MonthlyValues, x.TotalNoVAT, x.TotalWithVAT, profileSummary.IncludeVat));

        var openingDebtors = categories.Sum(x => x.OpeningDebtorsAmount);
        var openingCreditors = categories.Sum(x => x.OpeningCreditorsAmount);
        var collections = BuildSchedule(salesByMonth, debtorBuckets, profileSummary.BadDebtPercentage);
        AddOpeningBalanceSchedule(collections, openingDebtors, debtorBuckets);
        var payments = BuildSchedule(purchasesByMonth, creditorBuckets, 0m);
        AddOpeningBalanceSchedule(payments, openingCreditors, creditorBuckets);

        var monthlyResults = BuildMonthlyResults(
            salesByMonth,
            purchasesByMonth,
            collections,
            payments,
            openingDebtors,
            openingCreditors,
            profileSummary.BadDebtPercentage);
        var categoryResults = BuildCategoryResults(categories, sales, stock, profileSummary.IncludeVat);

        AddDataAlerts(
            alerts,
            categories.Count,
            sales.Count,
            stock.Count,
            profileSummary,
            monthlyResults);

        _logger.LogInformation(
            "Calculated accounts projection for assessment {AssessmentId}: {SalesCount} sales rows, {StockCount} stock rows",
            assessmentId,
            sales.Count,
            stock.Count);

        return new AccountsProjectionResult
        {
            AssessmentId = assessmentId,
            Profile = profileSummary,
            Months = monthlyResults,
            Categories = categoryResults,
            Alerts = alerts
        };
    }

    private static AccountsProfileSummary BuildProfile(
        DebtorsCreditorsProfile? profile,
        ICollection<AccountsProjectionAlert> alerts)
    {
        if (profile is null)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Critical,
                "Profile not configured",
                "Add an active debtors and creditors profile before relying on these projections."));

            return new AccountsProfileSummary
            {
                DebtorBuckets = EmptyBuckets(),
                CreditorBuckets = EmptyBuckets()
            };
        }

        return new AccountsProfileSummary
        {
            IsConfigured = true,
            IncludeVat = profile.IncludeVAT,
            BadDebtPercentage = Math.Clamp(profile.BadDebtPercentage, 0m, 100m),
            AveragePaymentDays = Math.Max(profile.AveragePaymentDays, 0),
            DebtorBuckets = CreateBuckets(
                CalculateFirstBucket(
                    profile.Debtors_60,
                    profile.Debtors_90,
                    profile.Debtors_120,
                    profile.Debtors_120Plus),
                profile.Debtors_60,
                profile.Debtors_90,
                profile.Debtors_120,
                profile.Debtors_120Plus),
            CreditorBuckets = CreateBuckets(
                CalculateFirstBucket(
                    profile.Creditors_60,
                    profile.Creditors_90,
                    profile.Creditors_120,
                    profile.Creditors_120Plus),
                profile.Creditors_60,
                profile.Creditors_90,
                profile.Creditors_120,
                profile.Creditors_120Plus)
        };
    }

    private static decimal CalculateFirstBucket(
        decimal days60,
        decimal days90,
        decimal days120,
        decimal days120Plus) =>
        Math.Max(0m, 100m - days60 - days90 - days120 - days120Plus);

    private static IReadOnlyList<MonthlyAccountsProjection> BuildMonthlyResults(
        IReadOnlyList<decimal> sales,
        IReadOnlyList<decimal> purchases,
        IReadOnlyList<decimal> collections,
        IReadOnlyList<decimal> payments,
        decimal initialDebtors,
        decimal initialCreditors,
        decimal badDebtPercentage)
    {
        var results = new List<MonthlyAccountsProjection>(ProjectionMonths);
        var openingDebtors = initialDebtors;
        var openingCreditors = initialCreditors;

        for (var month = 0; month < ProjectionMonths; month++)
        {
            var badDebt = sales[month] * badDebtPercentage / 100m;
            var closingDebtors = Math.Max(
                0m,
                openingDebtors + sales[month] - collections[month] - badDebt);
            var closingCreditors = Math.Max(
                0m,
                openingCreditors + purchases[month] - payments[month]);

            results.Add(new MonthlyAccountsProjection
            {
                MonthNumber = month + 1,
                OpeningDebtors = openingDebtors,
                CreditSales = sales[month],
                DebtorCollections = collections[month],
                BadDebt = badDebt,
                ClosingDebtors = closingDebtors,
                OpeningCreditors = openingCreditors,
                CreditPurchases = purchases[month],
                CreditorPayments = payments[month],
                ClosingCreditors = closingCreditors
            });

            openingDebtors = closingDebtors;
            openingCreditors = closingCreditors;
        }

        return results;
    }

    private static IReadOnlyList<CategoryAccountsProjection> BuildCategoryResults(
        IEnumerable<AssessmentSalesCategory> categories,
        IEnumerable<AssessmentSales> sales,
        IEnumerable<AssessmentStock> stock,
        bool includeVat)
    {
        var salesList = sales.ToList();
        var stockList = stock.ToList();

        return categories
            .Select(category => new CategoryAccountsProjection
            {
                CategoryId = category.AssessmentSalesCategoryId,
                CategoryName = category.AssessmentSalesCategoryName ?? $"Category {category.AssessmentSalesCategoryId}",
                OpeningDebtors = category.OpeningDebtorsAmount,
                OpeningCreditors = category.OpeningCreditorsAmount,
                AnnualSales = salesList
                    .Where(x => x.ProductCategoryId == category.AssessmentSalesCategoryId)
                    .Sum(x => AnnualAmount(x.MonthlyValues, x.TotalNoVAT, x.TotalWithVAT, includeVat)),
                AnnualPurchases = stockList
                    .Where(x => x.AssessmentSalesCategoryId == category.AssessmentSalesCategoryId)
                    .Sum(x => AnnualAmount(x.MonthlyValues, x.TotalNoVAT, x.TotalWithVAT, includeVat))
            })
            .OrderByDescending(x => x.AnnualSales)
            .ThenBy(x => x.CategoryName)
            .ToList();
    }

    private static void AddDataAlerts(
        ICollection<AccountsProjectionAlert> alerts,
        int categoryCount,
        int salesCount,
        int stockCount,
        AccountsProfileSummary profile,
        IReadOnlyList<MonthlyAccountsProjection> months)
    {
        if (categoryCount == 0)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Critical,
                "No active sales categories",
                "Opening balances and category summaries cannot be calculated."));
        }

        if (salesCount == 0)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Warning,
                "No monthly sales",
                "The debtor projection contains opening balances only."));
        }

        if (stockCount == 0)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Warning,
                "No monthly stock purchases",
                "The creditor projection contains opening balances only."));
        }

        AddPercentageAlert(alerts, "Debtor", profile.DebtorPercentageTotal);
        AddPercentageAlert(alerts, "Creditor", profile.CreditorPercentageTotal);

        if (profile.BadDebtPercentage >= 10m)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Critical,
                "High bad-debt assumption",
                $"The bad-debt rate is {profile.BadDebtPercentage:N1}%."));
        }
        else if (profile.BadDebtPercentage >= 5m)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Warning,
                "Elevated bad-debt assumption",
                $"The bad-debt rate is {profile.BadDebtPercentage:N1}%."));
        }

        var pressureMonths = months.Count(x => x.ClosingCreditors > x.ClosingDebtors);
        if (pressureMonths >= 6)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Warning,
                "Working-capital pressure",
                $"Closing creditors exceed closing debtors in {pressureMonths} of 12 months."));
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new(
                AccountsAlertSeverity.Information,
                "Projection data is complete",
                "Profiles total 100% and all core data sources contain active records."));
        }
    }

    private static void AddPercentageAlert(
        ICollection<AccountsProjectionAlert> alerts,
        string label,
        decimal total)
    {
        if (total == 100m)
        {
            return;
        }

        alerts.Add(new(
            total == 0m ? AccountsAlertSeverity.Critical : AccountsAlertSeverity.Warning,
            $"{label} profile totals {total:N1}%",
            total == 0m
                ? $"Configure the {label.ToLowerInvariant()} profile; no schedule can be produced."
                : "The profile is normalized to 100% for this projection. Correct the saved percentages."));
    }

    private static decimal[] BuildSchedule(
        IReadOnlyList<decimal> source,
        IReadOnlyList<ProfileBucket> normalizedBuckets,
        decimal deductionPercentage)
    {
        var schedule = new decimal[ProjectionMonths];
        var distributableFactor = 1m - Math.Clamp(deductionPercentage, 0m, 100m) / 100m;

        for (var sourceMonth = 0; sourceMonth < ProjectionMonths; sourceMonth++)
        {
            foreach (var bucket in normalizedBuckets)
            {
                var destinationMonth = sourceMonth + bucket.DelayMonths;
                if (destinationMonth < ProjectionMonths)
                {
                    schedule[destinationMonth] +=
                        source[sourceMonth] * distributableFactor * bucket.Percentage / 100m;
                }
            }
        }

        return schedule;
    }

    private static void AddOpeningBalanceSchedule(
        decimal[] schedule,
        decimal openingBalance,
        IReadOnlyList<ProfileBucket> normalizedBuckets)
    {
        foreach (var bucket in normalizedBuckets)
        {
            if (bucket.DelayMonths < ProjectionMonths)
            {
                schedule[bucket.DelayMonths] += openingBalance * bucket.Percentage / 100m;
            }
        }
    }

    private static IReadOnlyList<ProfileBucket> Normalize(IReadOnlyList<ProfileBucket> buckets)
    {
        var total = buckets.Sum(x => Math.Max(x.Percentage, 0m));
        if (total <= 0m)
        {
            return buckets.Select(x => x with { Percentage = 0m }).ToList();
        }

        return buckets
            .Select(x => x with { Percentage = Math.Max(x.Percentage, 0m) * 100m / total })
            .ToList();
    }

    private static IReadOnlyList<ProfileBucket> CreateBuckets(
        decimal days30,
        decimal days60,
        decimal days90,
        decimal days120,
        decimal days120Plus) =>
    [
        new("0-30 days", 0, days30),
        new("31-60 days", 1, days60),
        new("61-90 days", 2, days90),
        new("91-120 days", 3, days120),
        new("120+ days", 4, days120Plus)
    ];

    private static IReadOnlyList<ProfileBucket> EmptyBuckets() =>
        CreateBuckets(0m, 0m, 0m, 0m, 0m);

    private static decimal[] SumMonthly<T>(
        IEnumerable<T> rows,
        Func<T, decimal[]> rawSelector,
        Func<T, decimal[]> adjustedSelector)
    {
        var totals = new decimal[ProjectionMonths];
        foreach (var row in rows)
        {
            var raw = rawSelector(row);
            var values = adjustedSelector(row);
            if (raw.Length != ProjectionMonths || values.Length != ProjectionMonths)
            {
                throw new InvalidOperationException("Monthly projection data must contain exactly 12 values.");
            }

            for (var month = 0; month < ProjectionMonths; month++)
            {
                totals[month] += values[month];
            }
        }

        return totals;
    }

    private static decimal AnnualAmount(
        decimal[] monthlyValues,
        decimal totalNoVat,
        decimal totalWithVat,
        bool includeVat) =>
        VatAdjustedMonthlyValues(monthlyValues, totalNoVat, totalWithVat, includeVat).Sum();

    private static decimal[] VatAdjustedMonthlyValues(
        decimal[] monthlyValues,
        decimal totalNoVat,
        decimal totalWithVat,
        bool includeVat)
    {
        if (!includeVat || totalNoVat <= 0m || totalWithVat <= 0m)
        {
            return monthlyValues;
        }

        var vatFactor = totalWithVat / totalNoVat;
        return monthlyValues.Select(x => x * vatFactor).ToArray();
    }
}
