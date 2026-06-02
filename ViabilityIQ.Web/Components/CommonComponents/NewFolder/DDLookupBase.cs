using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BrucolWeb.Web.Components.Common
{
    public abstract class DDLookupBase<TValue, TItem>  : ComponentBase, IDisposable
    {
        [Parameter] public TValue Value { get; set; }
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
        [Inject]  protected IJSRuntime JS { get; set; } = default!;

        protected ElementReference dropdownRef;

        private DotNetObjectReference<DDLookupBase<TValue, TItem>>? _objRef;

        protected List<TItem> Items = new();

        protected List<TItem> FilteredItems = new();

        protected bool IsOpen = false;

        protected int SelectedIndex = -1;

        protected int CurrentSelectedIndex = -1;

        protected string _searchText = "";

        protected string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                Filter();
            }
        }

        protected abstract string GetText(TItem item);
        protected abstract TValue GetValue(TItem item);

        protected virtual void Filter()
        {
            FilteredItems = Items
                .Where(x =>
                    GetText(x)
                     .Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            SelectedIndex = -1;
        }

        protected virtual void ShowDropdown()
        {
            FilteredItems = Items.ToList();

            SelectedIndex = CurrentSelectedIndex;

            IsOpen = true;
        }

        protected virtual async Task SelectItem(TItem item)
        {
            Value = GetValue(item);

            await ValueChanged.InvokeAsync(Value);

            SearchText = GetText(item);

            CurrentSelectedIndex =
                Items.FindIndex(x =>
                    EqualityComparer<TValue>.Default.Equals(
                        GetValue(x),
                        Value));

            IsOpen = false;
        }

        protected virtual void HandleKeyDown(
            KeyboardEventArgs e)
        {
            if (!IsOpen) return;

            if (e.Key == "ArrowDown")
            {
                SelectedIndex =
                    Math.Min(
                      SelectedIndex + 1,
                      FilteredItems.Count - 1);
            }

            else if (e.Key == "ArrowUp")
            {
                SelectedIndex =
                    Math.Max(
                      SelectedIndex - 1,
                      0);
            }

            else if (e.Key == "Enter")
            {
                if (SelectedIndex >= 0)  SelectItem(FilteredItems[SelectedIndex]);
            }

            else if (e.Key == "Escape")
            {
                IsOpen = false;
            }
        }

        [JSInvokable]
        public void CloseDropdown()
        {
            IsOpen = false;

            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(
            bool firstRender)
        {
            if (firstRender)
            {
                _objRef =
                  DotNetObjectReference.Create(this);

                await JS.InvokeVoidAsync(
                  "zabDropdown.registerClickOutside",
                  dropdownRef,
                  _objRef);
            }
        }

        public void Dispose()
        {
            _objRef?.Dispose();
        }
    }
}
