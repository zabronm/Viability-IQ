using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BrucolWeb.Domain.Enums;

namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabDDLookupEnum<TEnum> : IDisposable
        where TEnum : struct, Enum
    {
        public class EnumOption
        {
            public TEnum Value { get; set; }
            public string Text { get; set; } = "";
        }

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Parameter]
        public TEnum Value { get; set; }

        [Parameter]
        public EventCallback<TEnum> ValueChanged { get; set; }

        [Parameter]
        public string Placeholder { get; set; } = "-- Select --";

        [Parameter]
        public string Label { get; set; } = "";

        private ElementReference dropdownRef;

        private DotNetObjectReference<ZabDDLookupEnum<TEnum>>? _objRef;

        private List<EnumOption> Items = new();
        private List<EnumOption> FilteredItems = new();

        private bool IsOpen;

        private int SelectedIndex = -1;
        private int CurrentSelectedIndex = -1;

        private string _searchText = "";

        private string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                Filter();
            }
        }

        protected override Task OnInitializedAsync()
        {
            Items = Enum.GetValues<TEnum>()
                .Select(x => new EnumOption
                {
                    Value = x,
                    Text = ((Enum)(object)x).GetDisplayName()
                })
                .ToList();

            FilteredItems = Items.ToList();

            CurrentSelectedIndex =
                Items.FindIndex(x =>
                    EqualityComparer<TEnum>.Default.Equals(
                        x.Value,
                        Value));

            var selected =
                Items.FirstOrDefault(x =>
                    EqualityComparer<TEnum>.Default.Equals(
                        x.Value,
                        Value));

            if (selected != null)
                SearchText = selected.Text;

            return Task.CompletedTask;
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

        private void Filter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredItems = Items.ToList();
            }
            else
            {
                FilteredItems = Items
                    .Where(x =>
                        x.Text.Contains(
                            SearchText,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            SelectedIndex = -1;
        }

        private void ShowDropdown()
        {
            FilteredItems = Items.ToList();

            SelectedIndex = CurrentSelectedIndex;

            IsOpen = true;
        }

        private async Task SelectItem(EnumOption item)
        {
            Value = item.Value;

            await ValueChanged.InvokeAsync(Value);

            SearchText = item.Text;

            CurrentSelectedIndex =
                Items.FindIndex(x =>
                    EqualityComparer<TEnum>.Default.Equals(
                        x.Value,
                        item.Value));

            IsOpen = false;
        }

        private void HandleKeyDown(
            KeyboardEventArgs e)
        {
            if (!IsOpen)
                return;

            if (e.Key == "ArrowDown")
            {
                SelectedIndex = Math.Min(
                    SelectedIndex + 1,
                    FilteredItems.Count - 1);
            }
            else if (e.Key == "ArrowUp")
            {
                SelectedIndex = Math.Max(
                    SelectedIndex - 1,
                    0);
            }
            else if (e.Key == "Enter")
            {
                if (SelectedIndex >= 0)
                {
                    _ = SelectItem(
                        FilteredItems[SelectedIndex]);
                }
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

        public void Dispose()
        {
            _objRef?.Dispose();
        }
    }
}