

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BrucolWeb.Web.Components.Common // 🔹 change to your namespace
{
    public partial class ZabDataTable_Docs<TItem>
    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }

        // 🔹 Edit event callback
        [Parameter] public EventCallback<TItem> OnEdit { get; set; }
        [Parameter] public EventCallback<TItem> OnEditDocs { get; set; }

        [Parameter] public EventCallback<TItem> OnDelete { get; set; }
        [Parameter] public EventCallback OnPrint { get; set; }
        [Parameter] public bool ShowDelete { get; set; } = false;
        [Parameter] public EventCallback OnAdd { get; set; }
        [Parameter] public List<TItem> Items { get; set; } = new();
        [Parameter] public List<ColumnDefinition<TItem>> Columns { get; set; } = new();
        [Parameter] public bool blShowButton { get; set; } = true;         //set default to true since most calls require it by default

        private string _searchText = "";


        protected string searchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                currentPage = 1; // reset paging on search
            }
        }

        protected int currentPage = 1;
        protected int pageSize = 10;

        protected string? sortColumn;
        protected bool sortAsc = true;

        // 🔹 Filtering
        protected IEnumerable<TItem> FilteredItems =>
            string.IsNullOrWhiteSpace(searchText)
                ? Items
                : Items.Where(item =>
                    Columns.Any(c =>
                        c.Value(item)?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true));

        // 🔹 Sorting
        protected IEnumerable<TItem> SortedItems =>
            sortColumn == null
                ? FilteredItems
                : (sortAsc
                    ? FilteredItems.OrderBy(x => GetValue(x))
                    : FilteredItems.OrderByDescending(x => GetValue(x)));

        // 🔹 Paging
        protected IEnumerable<TItem> PagedItems =>
            SortedItems.Skip((currentPage - 1) * pageSize).Take(pageSize);

        protected int totalPages =>
            Math.Max(1, (int)Math.Ceiling((double)FilteredItems.Count() / pageSize));

        // 🔹 Sorting logic
        protected void Sort(ColumnDefinition<TItem> col)
        {
            if (sortColumn == col.Title)
                sortAsc = !sortAsc;
            else
            {
                sortColumn = col.Title;
                sortAsc = true;
            }
        }

        protected object? GetValue(TItem item)
        {
            var col = Columns.FirstOrDefault(c => c.Title == sortColumn);
            return col?.Value(item);
        }

        protected string GetSortClass(ColumnDefinition<TItem> col)
        {
            if (sortColumn != col.Title) return "";
            return sortAsc ? "sort-asc" : "sort-desc";
        }

        // 🔹 Pagination
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

        // 🔹 Edit click handler
        protected async Task OnEditClick(TItem item)
        {
            if (OnEdit.HasDelegate)
                await OnEdit.InvokeAsync(item);
        }

        // 🔹 Edit click handler for documents checklist
        protected async Task HandleEditDocsClick(TItem item)
        {
            if (OnEditDocs.HasDelegate)
                await OnEditDocs.InvokeAsync(item);
        }

        protected async Task HandleEditClick(TItem item)
        {
            if (OnEdit.HasDelegate)
                await OnEdit.InvokeAsync(item);
        }


        // 🔹 Column definition
        public class ColumnDefinition<T>
        {
            public string Title { get; set; } = string.Empty;
            public Func<T, object?> Value { get; set; } = default!;
        }

        //Confirm button if you have to delete the selected entry
        protected async Task ConfirmDelete(TItem item)
        {
            if (await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this record?"))
            {
                if (OnDelete.HasDelegate)
                    await OnDelete.InvokeAsync(item);
            }
        }

        //Handle the Add event
        protected async Task OnAddClick()
        {
            if (OnAdd.HasDelegate)
                await OnAdd.InvokeAsync();
        }

        //Handle the Add event
        protected async Task OnPrintClick()
        {
            if (OnPrint.HasDelegate)
            {
                await OnPrint.InvokeAsync();
            }           
        }


        protected void ClearSearch() => searchText = string.Empty;      //clear search and restart

    }
}