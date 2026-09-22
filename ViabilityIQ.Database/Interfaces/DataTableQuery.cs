namespace ViabilityIQ.Application.Interfaces;

public sealed class DataTableQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchText { get; init; }
    public IReadOnlyList<string> SearchFields { get; init; } = [];
    public string? SortField { get; init; }
    public bool SortDescending { get; init; }
    public IReadOnlyDictionary<string, string> Filters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class DataTablePage<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
