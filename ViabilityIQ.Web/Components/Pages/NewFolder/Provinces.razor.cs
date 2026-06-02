using BrucolWeb.Application.DTOs.CropCategories;
using BrucolWeb.Application.DTOs.Provinces;
using BrucolWeb.Application.DTOs.Races;
using BrucolWeb.Application.Services;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Data;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class Provinces
    {       
        [Inject] protected ProvinceService? provinceService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateProvinceDTO> OnSave { get; set; }

        private CreateProvinceDTO _model = new CreateProvinceDTO();
        private List<ProvinceListDTO> provinces   = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Province";
        private bool isSaving = false;
        private string? ButtonMessage;
        bool isLoading = true;


        List<ZabDataTable<ProvinceListDTO>.ColumnDefinition<ProvinceListDTO>> columns = new()
        {
            new() { Title = "Province", Value = x => x.ProvinceName },
            new() { Title = "Short Name", Value = x => x.ShortName },
            new() { Title = "Remarks", Value = x => x.Remarks },
            new() { Title = "Active", Value = x => x.Active ==true? "Yes":"No"},
        };

        protected override async Task OnInitializedAsync()
        {
            await RefreshDataAsync();
        }

        async Task RefreshDataAsync()
        {
            try
            {
                isLoading = true;
                var result = await provinceService.GetListAsync();
                provinces = result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        async Task CreateProvince()
        {
            //Nav?.NavigateTo("/CropPages/create");
            ButtonMessage = "Create Province";
            offcanvasTitle = "Add Province";
            _model = new CreateProvinceDTO();
            await offcanvas.Show();
        }

        void EditProvinceDetails(long id) => Nav.NavigateTo($"/provincePages/edit/{id}");


        //EDIT PROVINCE IN OFF-CANVAS
        async Task EditProvince(ProvinceListDTO province )
        {
            //EditBankDetails(bank.Id);
            ButtonMessage = "Update Province";
            offcanvasTitle = "Edit Province";
            _model = new CreateProvinceDTO
            {
                ProvinceName = province.ProvinceName,
                ShortName = province.ShortName,
                Remarks = province.Remarks,
                Active = province.Active
            };

            await offcanvas.Show();
        }

        async Task DeleteProvince(ProvinceListDTO province  )
        {
            try
            {
                //await bankService.ArchiveBank(bank.Id);
                //farms.Remove(crop);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
        }

        //SAVE PROVINCE CODE
        async Task SaveProvince ()
        {
            try
            {
                //await bankService.ArchiveBank(bank.Id);
                //banks.Remove(bank);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
        }
            
        private async Task HandleSubmit()
        {
            isSaving = true;

            await OnSave.InvokeAsync(_model);

            isSaving = false;
        }

    }
}
