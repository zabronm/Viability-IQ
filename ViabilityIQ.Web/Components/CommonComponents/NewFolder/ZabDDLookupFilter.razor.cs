using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using BrucolWeb.Application.DTOs.Common;
using BrucolWeb.Application.Services;
using BrucolWeb.Web.Components.Common.Base;


namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabDDLookupFilter<TValue> : ZabSearchDropdownBase<TValue, DDLookupDTO>
    {
        [Inject] DDLookupService ddLookupService { get; set; } = default!;
        [Parameter] public string LookupType { get; set; } = "";
        [Parameter] public string Placeholder { get; set; } = "-- Select --";
        [Parameter] public string Label { get; set; } = "";

        protected override string GetText(DDLookupDTO item) => item.Description;
        protected string TestValue = "Hello";
        protected override TValue GetValue(DDLookupDTO item)
            => (TValue)Convert.ChangeType(item.ItemId, typeof(TValue));

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;

            Items = new();
            var x = TestValue;

            Items = LookupType switch
            {
                "Banks" => (await ddLookupService.GetBanksAsync()).ToList(),
                "Provinces" => (await ddLookupService.GetProvincesAsync()).ToList(),
                "Companies" => (await ddLookupService.getCompaniesAsync()).ToList(),
                "Races" => (await ddLookupService.GetRaceAsync()).ToList(),
                "Genders" => (await ddLookupService.GetGenderAsync()).ToList(),
                "Farms" => (await ddLookupService.GetFarmsAsync()).ToList(),
                "CropCategories" => (await ddLookupService.GetCropCategorysAsync()).ToList(),
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
    }
}
    
