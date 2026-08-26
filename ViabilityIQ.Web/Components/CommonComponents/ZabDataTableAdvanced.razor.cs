using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.CommonComponents
{
    public partial class ZabDataTableAdvanced<TItem>
    {
        [Inject] ISessionService? ZabSession { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] NavigationManager Nav { get; set; } = default!;
        [Inject] OffCanvasStateService OffcanvasService { get; set; } = default!;

        [Parameter] public Type? FormComponent { get; set; }                //This will accept the form component to be opened when add/edit button is clicked

        // Configuration Parameter Flags
        [Parameter] public bool ShowDelete { get; set; } = false;
        [Parameter] public bool ShowAdd { get; set; } = true;
        [Parameter] public bool IsRowClickable { get; set; } = false;
        [Parameter] public bool IsLoading { get; set; } = false;

        // Custom Component Workflow Handling Hooks
        [Parameter] public bool ShowSecondaryAction { get; set; } = false;
        [Parameter] public string? ItemName { get; set; } ="";
        [Parameter] public string SecondaryActionIcon { get; set; } = "bi bi-journal-check";
        [Parameter] public string SecondaryActionTitle { get; set; } = "Process Entry";
        [Parameter] public EventCallback<long> OnSecondaryAction { get; set; }

        // Core Collection Engine Parameters
        [Parameter] public List<TItem> Items { get; set; } = new();
        [Parameter] public List<ColumnDefinition<TItem>> Columns { get; set; } = new();

        // Functional Callback Definitions
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




        // Core Internal State Parameters
        private string _searchText = string.Empty;
        private string searchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                currentPage = 1;
            }
        }

        private int currentPage { get; set; } = 1;
        [Parameter] public int PageSize { get; set; } = 10;

        private ColumnDefinition<TItem>? currentSortColumn;
        private bool isAscending = true;

        // Core Layout Computational Query Matrix
        private IEnumerable<TItem> FilteredItems
        {
            get
            {
                if (string.IsNullOrWhiteSpace(searchText)) return Items;

                return Items.Where(item =>
                    Columns.Any(col =>
                    {
                        var val = col.Value?.Invoke(item);
                        return val != null && val.ToString()!.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    })
                );
            }
        }

        private IEnumerable<TItem> SortedItems
        {
            get
            {
                if (currentSortColumn?.Value == null) return FilteredItems;

                return isAscending
                    ? FilteredItems.OrderBy(item => currentSortColumn.Value(item))
                    : FilteredItems.OrderByDescending(item => currentSortColumn.Value(item));
            }
        }

        private List<TItem> PagedItems
        {
            get
            {
                return SortedItems
                    .Skip((currentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
        }

        private int totalPages => (int)Math.Ceiling((double)FilteredItems.Count() / PageSize) == 0
            ? 1
            : (int)Math.Ceiling((double)FilteredItems.Count() / PageSize);

        // State Iteration Actions
        private void NextPage() { if (currentPage < totalPages) currentPage++; }
        private void PrevPage() { if (currentPage > 1) currentPage--; }

        private void Sort(ColumnDefinition<TItem> column)
        {
            if (column.Value == null) return;

            if (currentSortColumn == column)
            {
                isAscending = !isAscending;
            }
            else
            {
                currentSortColumn = column;
                isAscending = true;
            }
            currentPage = 1;
        }

        private string GetSortClass(ColumnDefinition<TItem> column)
        {
            if (currentSortColumn != column) return "";
            return isAscending ? "sort-asc" : "sort-desc";
        }

        private string ResolveCssClass(ColumnDefinition<TItem> column, TItem item)
        {
            if (column.CssClassSelector != null) return column.CssClassSelector(item);
            return column.CssClass;
        }

        private string FormatCellValue(ColumnDefinition<TItem> column, TItem item)
        {
            if (column.Value == null) return string.Empty;
            var rawValue = column.Value(item);

            if (column.Formatter != null) return column.Formatter(rawValue);
            if (!string.IsNullOrWhiteSpace(column.FormatString) && rawValue is IFormattable formattable)
            {
                return formattable.ToString(column.FormatString, null);
            }

            return rawValue?.ToString() ?? string.Empty;
        }

        private async Task HandleRowClick(TItem item)
        {
            if (OnRowClick != null) await OnRowClick(item);
        }

        private async Task HandleLinkClick(ColumnDefinition<TItem> column, TItem item)
        {
            if (column.OnClick != null) await column.OnClick(item);
        }

        private async Task ConfirmDelete(TItem item)
        {
            if (OnDelete.HasDelegate) await OnDelete.InvokeAsync(item);
        }


        // Operational Execution Instances (Moved out of ColumnDefinition nested class)
        private async Task OnAddClick()
        {
            //if (OnAddRecordId.HasDelegate)
            //{
            //    await OnAddRecordId.InvokeAsync(0);
            //}
            if (OnAddRecordId.HasDelegate)
            {
                // This fires your ClientPage's HandleFormExecution(0) method
                await OnAddRecordId.InvokeAsync(0);
            }
            else if (FormComponent != null)
            {
                // Fallback if no explicit callback is bound
                await OffcanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = $"Add {ItemName}",
                        Width = 550,
                        ComponentType = FormComponent,
                        Parameters = new Dictionary<string, object?> { { "ClientId", 0L } }
                    });
            }
        }



        private async Task OnEditClick(object rawId)
        {
            if (OnEditRecordId.HasDelegate && rawId != null)
            {
                long id = Convert.ToInt64(rawId);
                await OnEditRecordId.InvokeAsync(id);
            }
        }



        // ==========================================================================
        // CALCULATE RECORD NUMBERS DYNAMIC
        // ==========================================================================

        private int StartRecordIndex
        {
            get
            {
                if (!FilteredItems.Any()) return 0;
                return ((currentPage - 1) * PageSize) + 1;
            }
        }

        private int EndRecordIndex
        {
            get
            {
                int calculatedEnd = currentPage * PageSize;
                int totalCount = FilteredItems.Count();
                return calculatedEnd > totalCount ? totalCount : calculatedEnd;
            }
        }
        //---------------------------------------------------------------------


        private async Task OnSecondaryActionClick(object rawId)
        {
            if (OnSecondaryAction.HasDelegate && rawId != null)
            {
                long id = Convert.ToInt64(rawId);
                await OnSecondaryAction.InvokeAsync(id);
            }
        }

        // ==========================================================================
        // SUB-STRUCT STRUCTURAL LAYOUT MAP DEFINITIONS
        // ==========================================================================
        public class ColumnDefinition<T>
        {
            public RenderFragment<TItem>? CellTemplate { get; set; }
            public string Title { get; set; } = string.Empty;
            public Func<T, object?>? Value { get; set; }

            // Action links rules
            public bool IsLink { get; set; } = false;
            public bool IsClickable { get; set; } = false;
            public Func<T, Task>? OnClick { get; set; }

            // Formatting
            public string? FormatString { get; set; }
            public Func<object?, string>? Formatter { get; set; }

            // Presentation CSS parameters
            public string CssClass { get; set; } = "";
            public Func<T, string>? CssClassSelector { get; set; }

            // Icon Elements
            public string? IconCss { get; set; }
            public bool IconOnly { get; set; }

            // Status Badge Systems
            public bool UseBadge { get; set; }
            public Func<T, string>? BadgeClass { get; set; }
        }

        private async Task HandlePrintTriggerAsync()
        {
            if (OnPrintList.HasDelegate)
            {
                // Extract only the current filtered/sorted entries, rather than the raw hidden dataset
                await OnPrintList.InvokeAsync(SortedItems.ToList());
            }
        }

        private async Task HandleExcelExportTriggerAsync()
        {
            if (OnExportExcelList.HasDelegate)
            {
                await OnExportExcelList.InvokeAsync(SortedItems.ToList());
            }
        }

        private async Task HandleEmailTriggerAsync()
        {
            if (OnEmailList.HasDelegate)
            {
                await OnEmailList.InvokeAsync(SortedItems.ToList());
            }
        }


    }
}