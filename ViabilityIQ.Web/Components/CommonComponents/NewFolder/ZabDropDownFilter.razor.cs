using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BrucolWeb.Application.DTOs.Common;
using BrucolWeb.Application.Services;

namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabDropDownFilter<TValue>
    {
        [Inject] DDLookupService ddLookupService { get; set; } = default!;
        [Inject] IJSRuntime JS { get; set; } = default!;

        [Parameter] public TValue Value { get; set; } = default!;
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

        [Parameter] public string LookupType { get; set; } = "";
        [Parameter] public string Placeholder { get; set; } = "-- Select --";
        [Parameter] public string Label { get; set; } = "";

        private ElementReference dropdownRef;

        private DotNetObjectReference<ZabDropDownFilter<TValue>>? _objRef;

        private List<DDLookupDTO> Items = new();
        private List<DDLookupDTO> FilteredItems = new();

        private bool IsOpen;
        private bool IsLoading;

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
        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;

            Items = LookupType switch
            {
                "Seasons" => (await ddLookupService.GetSeasonsAsync()).ToList(),
                "Banks" => (await ddLookupService.GetBanksAsync()).ToList(),
                "DocumentTypes" => (await ddLookupService.GetDocumentTypesAsync()).ToList(),
                "Provinces" => (await ddLookupService.GetProvincesAsync()).ToList(),
                "Companies" => (await ddLookupService.getCompaniesAsync()).ToList(),
                "Races" => (await ddLookupService.GetRaceAsync()).ToList(),
                "Genders" => (await ddLookupService.GetGenderAsync()).ToList(),
                "Farms" => (await ddLookupService.GetFarmsAsync()).ToList(),
                "Farmers" => (await ddLookupService.GetFarmersAsync()).ToList(),
                "FarmerTypes" => (await ddLookupService.GetFarmerTypesAsync()).ToList(),
                "LoanTypes" => (await ddLookupService.GetLoanCategoryAsync()).ToList(),
                "Crops" => (await ddLookupService.GetCropsAsync()).ToList(),
                "CropCategories" => (await ddLookupService.GetCropCategorysAsync()).ToList(),
                "Activities" => (await ddLookupService.GetActivitiesAsync()).ToList(),
                _ => new()
            };

            FilteredItems = Items.ToList();

            var selected = Items.FirstOrDefault(x => x.ItemId.Equals(Value));

            if (selected != null)
            {
                SearchText = selected.Description;

                CurrentSelectedIndex =
                    Items.FindIndex(x => x.ItemId.Equals(Value));
            }

            IsLoading = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objRef = DotNetObjectReference.Create(this);

                await JS.InvokeVoidAsync(
                    "zabDropdown.registerClickOutside",
                    dropdownRef,
                    _objRef);
            }
        }

        private void Filter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                FilteredItems = Items.ToList();
            else
                FilteredItems = Items
                    .Where(x => x.Description.Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

            SelectedIndex = -1;
        }
        private void ShowDropdown()
        {
            FilteredItems = Items.ToList();

            SelectedIndex = CurrentSelectedIndex;

            IsOpen = true;
        }

        private async Task SelectItem(DDLookupDTO item)
        {
            Value = (TValue)
                Convert.ChangeType(item.ItemId, typeof(TValue));

            await ValueChanged.InvokeAsync(Value);

            SearchText = item.Description;

            CurrentSelectedIndex =
                Items.FindIndex(x =>
                    x.ItemId.Equals(item.ItemId));

            IsOpen = false;
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (!IsOpen) return;

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
                    _ = SelectItem(FilteredItems[SelectedIndex]);
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