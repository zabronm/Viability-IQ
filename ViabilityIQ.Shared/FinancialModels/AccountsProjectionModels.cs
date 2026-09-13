namespace ViabilityIQ.Shared.FinancialModels;

public sealed class AccountsProjectionResult
{
    public long AssessmentId { get; init; }
    public AccountsProfileSummary Profile { get; init; } = new();
    public IReadOnlyList<MonthlyAccountsProjection> Months { get; init; } = [];
    public IReadOnlyList<CategoryAccountsProjection> Categories { get; init; } = [];
    public IReadOnlyList<AccountsProjectionAlert> Alerts { get; init; } = [];

    public decimal TotalSales => Months.Sum(x => x.CreditSales);
    public decimal TotalCollections => Months.Sum(x => x.DebtorCollections);
    public decimal TotalBadDebt => Months.Sum(x => x.BadDebt);
    public decimal TotalPurchases => Months.Sum(x => x.CreditPurchases);
    public decimal TotalPayments => Months.Sum(x => x.CreditorPayments);
    public decimal ClosingDebtors => Months.LastOrDefault()?.ClosingDebtors ?? 0m;
    public decimal ClosingCreditors => Months.LastOrDefault()?.ClosingCreditors ?? 0m;
    public decimal NetWorkingCapital => ClosingDebtors - ClosingCreditors;
}

public sealed class AccountsProfileSummary
{
    public bool IsConfigured { get; init; }
    public bool IncludeVat { get; init; }
    public decimal BadDebtPercentage { get; init; }
    public int AveragePaymentDays { get; init; }
    public IReadOnlyList<ProfileBucket> DebtorBuckets { get; init; } = [];
    public IReadOnlyList<ProfileBucket> CreditorBuckets { get; init; } = [];
    public decimal DebtorPercentageTotal => DebtorBuckets.Sum(x => x.Percentage);
    public decimal CreditorPercentageTotal => CreditorBuckets.Sum(x => x.Percentage);
}

public sealed record ProfileBucket(string Label, int DelayMonths, decimal Percentage);

public sealed class MonthlyAccountsProjection
{
    public int MonthNumber { get; init; }
    public decimal OpeningDebtors { get; init; }
    public decimal CreditSales { get; init; }
    public decimal DebtorCollections { get; init; }
    public decimal BadDebt { get; init; }
    public decimal ClosingDebtors { get; init; }
    public decimal OpeningCreditors { get; init; }
    public decimal CreditPurchases { get; init; }
    public decimal CreditorPayments { get; init; }
    public decimal ClosingCreditors { get; init; }
    public decimal NetCashMovement => DebtorCollections - CreditorPayments;
}

public sealed class CategoryAccountsProjection
{
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal OpeningDebtors { get; init; }
    public decimal OpeningCreditors { get; init; }
    public decimal AnnualSales { get; init; }
    public decimal AnnualPurchases { get; init; }
}

public enum AccountsAlertSeverity
{
    Information,
    Warning,
    Critical
}

public sealed record AccountsProjectionAlert(
    AccountsAlertSeverity Severity,
    string Title,
    string Message);
