using System.Globalization;
using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.CommonComponents;

public partial class ZabDataTableAdvanced<TItem> : IAsyncDisposable
{
    [Inject] private OffCanvasStateService OffcanvasService { get; set; } = default!;

    [Parameter] public Type? FormComponent { get; set; }
    [Parameter] public bool ShowDelete { get; set; }
    [Parameter] public bool ShowAdd { get; set; } = true;
    [Parameter] public bool IsRowClickable { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool ShowSecondaryAction { get; set; }
    [Parameter] public string ItemName { get; set; } = "Record";
    [Parameter] public string SearchPlaceholder { get; set; } = "Search records…";
    [Parameter] public string SecondaryActionIcon { get; set; } = "bi bi-journal-check";
    [Parameter] public string SecondaryActionTitle { get; set; } = "Process entry";
    [Parameter] public EventCallback<long> OnSecondaryAction { get; set; }

    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];
    [Parameter] public IReadOnlyList<ColumnDefinition<TItem>> Columns { get; set; } = [];
    [Parameter] public Func<DataTableQuery, CancellationToken, Task<DataTablePage<TItem>>>? ServerData { get; set; }
    [Parameter] public EventCallback<Exception> OnLoadError { get; set; }
    [Parameter] public string? DefaultSortField { get; set; }
    [Parameter] public bool DefaultSortDescending { get; set; }
    [Parameter] public int SearchDebounceMilliseconds { get; set; } = 350;
    [Parameter] public int MaximumExportRows { get; set; } = 10_000;

    [Parameter] public EventCallback<TItem> OnEdit { get; set; }
    [Parameter] public EventCallback<TItem> OnDelete { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public Func<TItem, Task>? OnRowClick { get; set; }
    [Parameter] public Func<TItem, object>? GetRowId { get; set; }
    [Parameter] public Func<TItem, string>? RowCssClassSelector { get; set; }
    [Parameter] public EventCallback<long> OnAddRecordId { get; set; }
    [Parameter] public EventCallback<long> OnEditRecordId { get; set; }
    [Parameter] public EventCallback<List<TItem>> OnPrintList { get; set; }
    [Parameter] public EventCallback<List<TItem>> OnExportExcelList { get; set; }
    [Parameter] public EventCallback<List<TItem>> OnEmailList { get; set; }

    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50, 100];

    private readonly string _utilityMenuId = $"zdt-output-{Guid.NewGuid():N}";
    private readonly string _pageSizeId = $"zdt-page-size-{Guid.NewGuid():N}";
    private readonly Dictionary<string, string> _filters = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<TItem> _serverItems = [];
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _debounceCancellation;
    private ColumnDefinition<TItem>? _currentSortColumn;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private int _currentPage = 1;
    private int _pageSize;
    private int _serverTotalCount;
    private int _requestVersion;
    private bool _serverInitialized;
    private bool _serverLoading;
    private bool _sortDescending;
    private bool ShowFilters { get; set; }

    private bool IsServerMode => ServerData is not null;
    private bool EffectiveLoading => IsLoading || _serverLoading;
    private bool ShowActions => ShowSecondaryAction || ShowDelete || GetRowId is not null;
    private int ColumnSpan => Columns.Count + (ShowActions ? 1 : 0);
    private bool HasActiveFilters => _filters.Values.Any(value => !string.IsNullOrWhiteSpace(value));
    private int TotalItemCount => IsServerMode ? _serverTotalCount : FilteredLocalItems.Count;
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItemCount / (double)_pageSize));
    private int StartRecordIndex => TotalItemCount == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;
    private int EndRecordIndex => Math.Min(_currentPage * _pageSize, TotalItemCount);
    private bool CanPrint => TotalItemCount > 0 && OnPrintList.HasDelegate;
    private bool CanExportExcel => TotalItemCount > 0 && OnExportExcelList.HasDelegate;
    private bool CanEmail => TotalItemCount > 0 && OnEmailList.HasDelegate;

    private IReadOnlyList<TItem> FilteredLocalItems
    {
        get
        {
            IEnumerable<TItem> result = Items;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                result = result.Where(item => Columns
                    .Where(column => column.Searchable)
                    .Any(column => Contains(column.Value?.Invoke(item), _searchText)));
            }

            foreach (var filter in _filters.Where(entry => !string.IsNullOrWhiteSpace(entry.Value)))
            {
                var column = Columns.FirstOrDefault(candidate =>
                    string.Equals(GetColumnKey(candidate), filter.Key, StringComparison.OrdinalIgnoreCase));
                if (column?.Value is not null)
                    result = result.Where(item => Contains(column.Value(item), filter.Value));
            }

            if (_currentSortColumn?.Value is not null)
            {
                result = _sortDescending
                    ? result.OrderByDescending(item => _currentSortColumn.Value(item), ObjectComparer.Instance)
                    : result.OrderBy(item => _currentSortColumn.Value(item), ObjectComparer.Instance);
            }

            return result.ToList();
        }
    }

    private IReadOnlyList<TItem> VisibleItems => IsServerMode
        ? _serverItems
        : FilteredLocalItems.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

    protected override void OnParametersSet()
    {
        if (_pageSize == 0)
            _pageSize = Math.Max(1, PageSize);
        if (_currentPage > TotalPages)
            _currentPage = TotalPages;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsServerMode && !_serverInitialized)
        {
            _serverInitialized = true;
            await LoadServerDataAsync();
        }
    }

    public Task RefreshAsync()
    {
        if (!IsServerMode)
        {
            StateHasChanged();
            return Task.CompletedTask;
        }

        return LoadServerDataAsync();
    }

    private async Task LoadServerDataAsync()
    {
        if (ServerData is null)
            return;

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        var requestVersion = ++_requestVersion;
        _serverLoading = true;
        _errorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ServerData(BuildQuery(_currentPage, _pageSize), cancellationToken);
            if (requestVersion != _requestVersion || cancellationToken.IsCancellationRequested)
                return;

            _serverItems = result.Items ?? [];
            _serverTotalCount = Math.Max(0, result.TotalCount);
            var actualTotalPages = Math.Max(1,
                (int)Math.Ceiling(_serverTotalCount / (double)_pageSize));
            if (_currentPage > actualTotalPages)
            {
                _currentPage = actualTotalPages;
                await LoadServerDataAsync();
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _serverItems = [];
            _serverTotalCount = 0;
            _errorMessage = $"Unable to load {ItemName.ToLowerInvariant()} data. Try again.";
            if (OnLoadError.HasDelegate)
                await OnLoadError.InvokeAsync(exception);
        }
        finally
        {
            if (requestVersion == _requestVersion)
            {
                _serverLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private DataTableQuery BuildQuery(int pageNumber, int pageSize) =>
        new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchText = NullIfWhiteSpace(_searchText),
            SearchFields = Columns
                .Where(column => column.Searchable && !string.IsNullOrWhiteSpace(column.ServerField))
                .Select(column => column.ServerField!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            SortField = _currentSortColumn?.ServerField ?? DefaultSortField,
            SortDescending = _currentSortColumn is null
                ? DefaultSortDescending
                : _sortDescending,
            Filters = Columns
                .Where(column => column.Filterable && !string.IsNullOrWhiteSpace(column.ServerField))
                .Select(column => new
                {
                    Field = column.ServerField!,
                    Value = GetFilterValue(column)
                })
                .Where(filter => !string.IsNullOrWhiteSpace(filter.Value))
                .ToDictionary(filter => filter.Field, filter => filter.Value,
                    StringComparer.OrdinalIgnoreCase)
        };

    private async Task HandleSearchInputAsync(ChangeEventArgs eventArgs)
    {
        _searchText = eventArgs.Value?.ToString() ?? string.Empty;
        _currentPage = 1;
        await DebounceReloadAsync();
    }

    private async Task HandleFilterInputAsync(
        ColumnDefinition<TItem> column,
        ChangeEventArgs eventArgs)
    {
        _filters[GetColumnKey(column)] = eventArgs.Value?.ToString() ?? string.Empty;
        _currentPage = 1;
        await DebounceReloadAsync();
    }

    private async Task DebounceReloadAsync()
    {
        if (!IsServerMode)
        {
            StateHasChanged();
            return;
        }

        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        _debounceCancellation = new CancellationTokenSource();
        var debounceCancellation = _debounceCancellation;
        try
        {
            await Task.Delay(
                Math.Clamp(SearchDebounceMilliseconds, 0, 2_000),
                debounceCancellation.Token);
            await LoadServerDataAsync();
        }
        catch (OperationCanceledException) when (debounceCancellation.IsCancellationRequested)
        {
        }
    }

    private async Task ClearSearchAsync()
    {
        _searchText = string.Empty;
        _currentPage = 1;
        if (IsServerMode)
            await LoadServerDataAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _filters.Clear();
        _currentPage = 1;
        if (IsServerMode)
            await LoadServerDataAsync();
        else
            StateHasChanged();
    }

    private void ToggleFilters() => ShowFilters = !ShowFilters;

    private async Task SortAsync(ColumnDefinition<TItem> column)
    {
        if (!column.Sortable || column.Value is null)
            return;

        if (_currentSortColumn == column)
            _sortDescending = !_sortDescending;
        else
        {
            _currentSortColumn = column;
            _sortDescending = false;
        }

        _currentPage = 1;
        if (IsServerMode)
            await LoadServerDataAsync();
    }

    private Task FirstPageAsync() => ChangePageAsync(1);
    private Task PreviousPageAsync() => ChangePageAsync(Math.Max(1, _currentPage - 1));
    private Task NextPageAsync() => ChangePageAsync(Math.Min(TotalPages, _currentPage + 1));
    private Task LastPageAsync() => ChangePageAsync(TotalPages);

    private async Task ChangePageAsync(int page)
    {
        if (page == _currentPage)
            return;
        _currentPage = page;
        if (IsServerMode)
            await LoadServerDataAsync();
    }

    private async Task ChangePageSizeAsync(ChangeEventArgs eventArgs)
    {
        if (!int.TryParse(eventArgs.Value?.ToString(), out var size) || size <= 0)
            return;
        _pageSize = size;
        _currentPage = 1;
        if (IsServerMode)
            await LoadServerDataAsync();
    }

    private string GetSortIcon(ColumnDefinition<TItem> column) =>
        _currentSortColumn != column
            ? "bi bi-arrow-down-up"
            : _sortDescending ? "bi bi-sort-down" : "bi bi-sort-up";

    private string GetAriaSort(ColumnDefinition<TItem> column) =>
        _currentSortColumn != column
            ? "none"
            : _sortDescending ? "descending" : "ascending";

    private bool CanSort(ColumnDefinition<TItem> column) =>
        column.Sortable
        && column.Value is not null
        && (!IsServerMode || !string.IsNullOrWhiteSpace(column.ServerField));

    private string GetFilterValue(ColumnDefinition<TItem> column) =>
        _filters.TryGetValue(GetColumnKey(column), out var value) ? value : string.Empty;

    private static string GetColumnKey(ColumnDefinition<TItem> column) =>
        column.ServerField ?? column.Title;

    private string ResolveCssClass(ColumnDefinition<TItem> column, TItem item)
    {
        var css = column.CssClassSelector?.Invoke(item) ?? column.CssClass;
        return string.Join(" ", new[] { "zdt-cell", css }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private string ResolveRowCssClass(TItem item) => RowCssClassSelector?.Invoke(item) ?? string.Empty;

    private static string FormatCellValue(ColumnDefinition<TItem> column, TItem item)
    {
        if (column.Value is null)
            return string.Empty;

        var rawValue = column.Value(item);
        if (column.Formatter is not null)
            return column.Formatter(rawValue);
        if (!string.IsNullOrWhiteSpace(column.FormatString) && rawValue is IFormattable formattable)
            return formattable.ToString(column.FormatString, CultureInfo.CurrentCulture);
        return rawValue?.ToString() ?? string.Empty;
    }

    private async Task HandleRowClick(TItem item)
    {
        if (IsRowClickable && OnRowClick is not null)
            await OnRowClick(item);
    }

    private async Task HandleLinkClick(ColumnDefinition<TItem> column, TItem item)
    {
        if (column.OnClick is not null)
            await column.OnClick(item);
    }

    private async Task ConfirmDelete(TItem item)
    {
        if (OnDelete.HasDelegate)
            await OnDelete.InvokeAsync(item);
    }

    private async Task OnAddClick()
    {
        if (OnAddRecordId.HasDelegate)
        {
            await OnAddRecordId.InvokeAsync(0);
            return;
        }
        if (OnAdd.HasDelegate)
        {
            await OnAdd.InvokeAsync();
            return;
        }
        if (FormComponent is not null)
        {
            await OffcanvasService.ShowAsync(new CanvasRequest
            {
                Title = $"Add {ItemName}",
                Width = 550,
                ComponentType = FormComponent,
                Parameters = new Dictionary<string, object?> { ["ClientId"] = 0L }
            });
        }
    }

    private async Task OnEditClick(object rawId)
    {
        if (OnEditRecordId.HasDelegate && rawId is not null)
            await OnEditRecordId.InvokeAsync(Convert.ToInt64(rawId, CultureInfo.InvariantCulture));
    }

    private async Task OnSecondaryActionClick(object rawId)
    {
        if (OnSecondaryAction.HasDelegate && rawId is not null)
            await OnSecondaryAction.InvokeAsync(Convert.ToInt64(rawId, CultureInfo.InvariantCulture));
    }

    private async Task<List<TItem>> GetOutputItemsAsync()
    {
        if (!IsServerMode || ServerData is null)
            return FilteredLocalItems.ToList();
        if (TotalItemCount > MaximumExportRows)
            throw new InvalidOperationException(
                $"The filtered result contains {TotalItemCount:N0} rows. Narrow it below {MaximumExportRows:N0} rows before export.");

        const int batchSize = 500;
        var output = new List<TItem>(TotalItemCount);
        var page = 1;
        while (output.Count < TotalItemCount)
        {
            var result = await ServerData(BuildQuery(page, batchSize), CancellationToken.None);
            output.AddRange(result.Items);
            if (result.Items.Count == 0)
                break;
            page++;
        }
        return output;
    }

    private async Task HandlePrintTriggerAsync() =>
        await ExecuteOutputAsync(OnPrintList);

    private async Task HandleExcelExportTriggerAsync() =>
        await ExecuteOutputAsync(OnExportExcelList);

    private async Task HandleEmailTriggerAsync() =>
        await ExecuteOutputAsync(OnEmailList);

    private async Task ExecuteOutputAsync(EventCallback<List<TItem>> callback)
    {
        if (!callback.HasDelegate)
            return;
        _errorMessage = null;
        try
        {
            await callback.InvokeAsync(await GetOutputItemsAsync());
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            if (OnLoadError.HasDelegate)
                await OnLoadError.InvokeAsync(exception);
        }
    }

    private static bool Contains(object? value, string search) =>
        value?.ToString()?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async ValueTask DisposeAsync()
    {
        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        if (_loadCancellation is not null)
        {
            await _loadCancellation.CancelAsync();
            _loadCancellation.Dispose();
        }
    }

    public sealed class ColumnDefinition<T>
    {
        public string Title { get; set; } = string.Empty;
        public string? ServerField { get; set; }
        public Func<T, object?>? Value { get; set; }
        public RenderFragment<T>? CellTemplate { get; set; }
        public bool Sortable { get; set; } = true;
        public bool Searchable { get; set; } = true;
        public bool Filterable { get; set; }
        public string? FilterPlaceholder { get; set; }
        public bool IsLink { get; set; }
        public bool IsClickable { get; set; }
        public Func<T, Task>? OnClick { get; set; }
        public string? FormatString { get; set; }
        public Func<object?, string>? Formatter { get; set; }
        public string CssClass { get; set; } = string.Empty;
        public string HeaderCssClass { get; set; } = string.Empty;
        public Func<T, string>? CssClassSelector { get; set; }
        public string? IconCss { get; set; }
        public bool IconOnly { get; set; }
        public bool UseBadge { get; set; }
        public Func<T, string>? BadgeClass { get; set; }
    }

    private sealed class ObjectComparer : IComparer<object?>
    {
        public static ObjectComparer Instance { get; } = new();

        public int Compare(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;
            if (left is IComparable comparable && left.GetType() == right.GetType())
                return comparable.CompareTo(right);
            return StringComparer.CurrentCultureIgnoreCase.Compare(left.ToString(), right.ToString());
        }
    }
}
