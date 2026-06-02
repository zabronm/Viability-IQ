using BrucolWeb.Application.DTOs.Common;
using BrucolWeb.Application.Services;
using Microsoft.AspNetCore.Components;
using System.Reflection.Metadata;

namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabDropDown<TValue> : ComponentBase
    {
        [Inject] private DDLookupService? ddLookupService { get; set; } = default!;

        [Parameter] public TValue? value { get; set; }
        [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

        [Parameter] public string LookupType { get; set; } = string.Empty;
        [Parameter] public string Placeholder { get; set; } = "Select options ...";

        protected List<DDLookupDTO> Items = new();
        protected bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                Items = LookupType switch
                {
                    "Banks" => (await ddLookupService!.GetBanksAsync()).ToList(),
                    "Provinces" => (await ddLookupService!.GetProvincesAsync()).ToList(),
                    //"Companies" => (await ddLookupService!.getCompaniesAsync()).ToList(),
                    "Races" => (await ddLookupService!.GetRaceAsync()).ToList(),
                    "Genders" => (await ddLookupService!.GetGenderAsync()).ToList(),
                    "Farms" => (await ddLookupService!.GetFarmsAsync()).ToList(),
                    "Farmers" => (await ddLookupService!.GetFarmersAsync()).ToList(),
                    "CropCategories" => (await ddLookupService!.GetCropCategorysAsync()).ToList(),
                    "LoanCategories" => (await ddLookupService!.GetLoanCategoryAsync()).ToList(),
                    "Applications" => (await ddLookupService!.GetApplicationsAsync()).ToList(),
                };
                isLoading = false;
            }
            catch (Exception ex)
            { }
        }
    }
}
