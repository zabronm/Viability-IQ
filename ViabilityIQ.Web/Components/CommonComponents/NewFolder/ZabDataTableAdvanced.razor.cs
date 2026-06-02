using BrucolWeb.Application.Interfaces.Common;
using BrucolWeb.Web.Services;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Data;
using System.Security.Cryptography;

namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabDataTableAdvanced<TItem>
    {
        [Inject] ZabSessionService? ZabSession { get; set; }
        [Inject] IDbService? dbService { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] NavigationManager Nav { get; set; } = default!;

        // PRESERVED original callbacks
        [Parameter] public EventCallback<TItem> OnEdit { get; set; }
        [Parameter] public EventCallback<TItem> OnDelete { get; set; }
        [Parameter] public EventCallback OnAdd { get; set; }
        [Parameter] public bool ShowDelete { get; set; } = false;
        [Parameter] public bool ShowAdd { get; set; } = true;

        [Parameter] public List<TItem> Items { get; set; } = new();
        [Parameter] public List<ColumnDefinition<TItem>> Columns { get; set; } = new();

        // NEW optional row click
        [Parameter] public Func<TItem, Task>? OnRowClick { get; set; }
        [Parameter] public Func<TItem, object>? GetRowId { get; set; }
        [Parameter] public Func<TItem, string>? EditUrl { get; set; }


        private string _searchText = "";

        protected string searchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                currentPage = 1;
            }
        }

        protected int currentPage = 1;
        protected int pageSize = 10;

        protected string? sortColumn;
        protected bool sortAsc = true;

        protected IEnumerable<TItem> FilteredItems =>
            string.IsNullOrWhiteSpace(searchText)
                ? Items
                : Items.Where(item =>
                    Columns.Any(c =>
                        c.Value(item)?
                         .ToString()?
                         .Contains(searchText,
                          StringComparison.OrdinalIgnoreCase) == true));

        protected IEnumerable<TItem> SortedItems =>
            sortColumn == null
                ? FilteredItems
                : sortAsc
                    ? FilteredItems.OrderBy(x => GetValue(x))
                    : FilteredItems.OrderByDescending(x => GetValue(x));

        protected IEnumerable<TItem> PagedItems => SortedItems.Skip((currentPage - 1) * pageSize).Take(pageSize);

        protected int totalPages => Math.Max(1, (int)Math.Ceiling((double)FilteredItems.Count() / pageSize));

        protected void Sort(ColumnDefinition<TItem> c)
        {
            if (sortColumn == c.Title)
                sortAsc = !sortAsc;
            else
            {
                sortColumn = c.Title;
                sortAsc = true;
            }
        }

        protected object? GetValue(TItem row)
        {
            var c = Columns.FirstOrDefault(x => x.Title == sortColumn);

            return c?.Value(row);
        }

        protected string GetSortClass(ColumnDefinition<TItem> c)
        {
            if (sortColumn != c.Title)
                return "";

            return sortAsc ? "sort-asc" : "sort-desc";
        }

        protected void NextPage()
        {
            if (currentPage < totalPages)
                currentPage++;
        }

        protected void PrevPage()
        {
            if (currentPage > 1)
                currentPage--;
        }

        // PRESERVED
        protected async Task OnEditClick(TItem item)
        {
            if (OnEdit.HasDelegate)
                await OnEdit.InvokeAsync(item);
        }

        protected async Task ConfirmDelete(TItem item)
        {
            if (await JSRuntime.InvokeAsync<bool>(
                "confirm",
                "Are you sure you want to delete this record?"))
            {
                if (OnDelete.HasDelegate)
                    await OnDelete.InvokeAsync(item);
            }
        }

        protected async Task OnAddClick()
        {
            if (OnAdd.HasDelegate)
                await OnAdd.InvokeAsync();
        }

        // NEW
        protected async Task HandleRowClick(TItem row)
        {
            if (OnRowClick != null)
                await OnRowClick(row);
        }

        protected async Task HandleLinkClick(ColumnDefinition<TItem> col, TItem row)
        {
            if (col.OnClick != null)
                await col.OnClick(row);
        }

        protected string FormatCellValue(ColumnDefinition<TItem> col, TItem row)
        {
            var v = col.Value(row);

            if (col.Formatter != null) return col.Formatter(v);

            if (!string.IsNullOrWhiteSpace(col.FormatString))
            {
                if (v is IFormattable f)
                    return f.ToString(col.FormatString, null);
            }

            return v?.ToString() ?? "";
        }

        protected string ResolveCssClass(ColumnDefinition<TItem> col, TItem row)
        {
            if (col.CssClassSelector != null) return col.CssClassSelector(row);
            return col.CssClass;
        }

        //This function handles the Edit button, given the URL and the record 's ID.
        //It performs validation and navigates to the edit page passing the ID.
        protected Task HandleEditButtonClick(TItem row)
        {

            // Check row itself          
            if (row == null)
            {
                Console.WriteLine("Supplied row has no values.");

                return Task.CompletedTask;
            }

            // Validate ID (optional safety)

            if (GetRowId != null)
            {
                var id = GetRowId(row);

                if (id == null)
                {
                    Console.WriteLine("Application ID is null.");

                    return Task.CompletedTask;
                }

                if (string.IsNullOrWhiteSpace(id.ToString()))
                {
                    Console.WriteLine("Application ID is empty.");

                    return Task.CompletedTask;
                }

                // Optional numeric safety
                if (int.TryParse(id.ToString(), out int numericId))
                {
                    if (numericId <= 0)
                    {
                        Console.WriteLine("Application ID invalid.");
                        return Task.CompletedTask;
                    }
                }
            }
            else
            {
                Console.WriteLine("GetRowId function not supplied.");

                return Task.CompletedTask;
            }

            if (EditUrl == null)
            {
                Console.WriteLine("EditUrl function not supplied.");

                return Task.CompletedTask;
            }

            var url = EditUrl(row);


            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("Generated URL is empty.");

                return Task.CompletedTask;
            }

            // Navigate         

            Nav.NavigateTo(url);

            return Task.CompletedTask;
        }

        public class ColumnDefinition<T>
        {
            public string Title { get; set; } = "";
            public Func<T, object?> Value { get; set; } = default!;

            // NEW links
            public bool IsLink { get; set; }
            public Func<T, Task>? OnClick { get; set; }

            // NEW formatting
            public string? FormatString { get; set; }
            public Func<object?, string>? Formatter { get; set; }

            // NEW css
            public string CssClass { get; set; } = "";
            public Func<T, string>? CssClassSelector { get; set; }

            // NEW icons
            public string? IconCss { get; set; }
            public bool IconOnly { get; set; }

            // NEW badges
            public bool UseBadge { get; set; }
            public Func<T, string>? BadgeClass { get; set; }
        }
    }
}