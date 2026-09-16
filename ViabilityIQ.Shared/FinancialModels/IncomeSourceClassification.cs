using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Shared.FinancialModels;

public enum IncomeSourceKind
{
    Sales,
    AssetDisposal,
    GrantsDonations,
    SundryIncome,
    OtherIncome
}

public sealed class IncomeTypeClassifier
{
    private readonly IReadOnlyDictionary<long, IncomeSourceKind> _classifications;

    public IncomeTypeClassifier(IEnumerable<IncomeType> incomeTypes)
    {
        ArgumentNullException.ThrowIfNull(incomeTypes);
        _classifications = incomeTypes
            .GroupBy(x => x.IncomeTypeId)
            .ToDictionary(x => x.Key, x => ClassifyName(x.First().IncomeTypeName));
    }

    public IncomeSourceKind Classify(AssessmentSales sale, AssessmentSalesCategory? category)
    {
        ArgumentNullException.ThrowIfNull(sale);

        if (category is not null
            && _classifications.TryGetValue(category.IncomeTypeId, out var categoryType))
        {
            return categoryType;
        }

        if (_classifications.TryGetValue(sale.IncomeTypeId, out var saleType))
        {
            return saleType;
        }

        return IncomeSourceKind.OtherIncome;
    }

    public IncomeSourceKind Classify(AssessmentSalesCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return _classifications.TryGetValue(category.IncomeTypeId, out var type)
            ? type
            : IncomeSourceKind.OtherIncome;
    }

    public bool IsKnown(long incomeTypeId) => _classifications.ContainsKey(incomeTypeId);

    private static IncomeSourceKind ClassifyName(string? value)
    {
        var name = Normalize(value);
        if (name.Contains("asset", StringComparison.Ordinal)
            && (name.Contains("disposal", StringComparison.Ordinal)
                || name.Contains("sale", StringComparison.Ordinal)))
        {
            return IncomeSourceKind.AssetDisposal;
        }

        if (name.StartsWith("sale", StringComparison.Ordinal))
        {
            return IncomeSourceKind.Sales;
        }

        if (name.Contains("grant", StringComparison.Ordinal)
            || name.Contains("donation", StringComparison.Ordinal))
        {
            return IncomeSourceKind.GrantsDonations;
        }

        if (name.Contains("sundry", StringComparison.Ordinal))
        {
            return IncomeSourceKind.SundryIncome;
        }

        return IncomeSourceKind.OtherIncome;
    }

    private static string Normalize(string? value) =>
        string.Concat((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit));
}
