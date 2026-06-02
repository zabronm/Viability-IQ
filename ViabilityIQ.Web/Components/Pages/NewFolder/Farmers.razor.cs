
using BrucolWeb.Application.DTOs.Crops;
using BrucolWeb.Application.DTOs.Farmers;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System.Data;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class Farmers
    {
        [Inject] protected FarmersService? farmersService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateFarmerDTO> OnSave { get; set; }

        private CreateFarmerDTO _model = new CreateFarmerDTO();
        private List<FarmerListDTO> farmers = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Client";
        private bool isSaving = false;
        private bool isLoading = false;
        private int? i_recs { get; set; } = 0;
        private long? dtoFarmerId { get; set; } = null;
        private string? ButtonMessage;


        List<ZabDataTable<FarmerListDTO>.ColumnDefinition<FarmerListDTO>> columns = new()
        {
            new() { Title = "Client Name", Value = x => x.FullName },
            new() { Title = "ID Number", Value = x => x.IDNumber },
            new() { Title = "Gender", Value = x => x.Gender },
            new() { Title = "Race", Value = x => x.Race },
            new() { Title = "SA ID", Value = x => x.SA_ID==true? "YES":"NO" },
            new() { Title = "Telephone", Value = x => x.Telephone },
            new() { Title = "Mobile", Value = x => x.Mobile },
            new() { Title = "Email", Value = x => x.Email },
            new() { Title = "Active", Value = x => x.Active==true? "Yes":"No" },
        };


        protected override async Task OnInitializedAsync()
        {
            await RefreshDataAsync();
        }


        private async Task RefreshDataAsync()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var result = await farmersService!.GetListAsync();
                farmers = result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Loading data ..", false);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }


        async Task CreateFarmer()
        {
            try
            {
                ButtonMessage = "Create Client";
                offcanvasTitle = "Add Client";
                dtoFarmerId = null;
                _model = new CreateFarmerDTO
                { Active = true, CreatedBy = 1, CreatedDate = DateTime.Now };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create farmer", false);
            }
        }

        void EditFarmerDetails(long id) => Nav.NavigateTo($"/farmers/edit/{id}");


        //EDIT FARM IN OFF-CANVAS
        async Task EditFarmer(FarmerListDTO farmer)
        {
            ButtonMessage = "Update Client";
            offcanvasTitle = "Edit Client";
            dtoFarmerId = farmer.FarmerId;      //this is very important
            _model = new CreateFarmerDTO
            {
                FullName = farmer.FullName,
                FarmerTypeId = farmer.FarmerTypeId,
                GenderId = farmer.GenderId,
                RaceId = farmer.RaceId,
                IDNumber = farmer.IDNumber,
                SA_ID = farmer.SA_ID,
                Address_Street = farmer.Address_Street,
                Address_Surburb = farmer.Address_Surburb,
                ProvinceId = farmer.ProvinceId,               
                Telephone = farmer.Telephone,
                Address_CityTown = farmer.Address_CityTown,
                Email = farmer.Email,               
                Mobile = farmer.Mobile,                
                Address_PostalCode = farmer.Address_PostalCode,                
                Address_PostalLocation = farmer.Address_PostalLocation,
                Address_Postal = farmer.Address_Postal,                             
                Remarks = farmer.Remarks,
                Active = farmer.Active,
                CreatedBy = farmer.CreatedBy,                
                CreatedDate = farmer.CreatedDate,
            };
            await offcanvas!.Show();
        }

        private async Task HandleSubmit()
        {
            if (farmersService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;

                if (dtoFarmerId.HasValue)
                {
                    var updateModel = new UpdateFarmerDTO
                    {
                        FullName = _model.FullName,                        
                        FarmerTypeId = _model.FarmerTypeId,
                        GenderId = _model.GenderId,
                        RaceId = _model.RaceId,
                        IDNumber = _model.IDNumber,                                             
                        Telephone = _model.Telephone,                        
                        Email = _model.Email,
                        Mobile = _model.Mobile,
                        Address_Street = _model.Address_Street,
                        Address_Surburb = _model.Address_Surburb,
                        Address_CityTown= _model.Address_CityTown,
                        ProvinceId = _model.ProvinceId,           
                        Address_Postal = _model.Address_Postal,
                        Address_PostalLocation =_model.Address_PostalLocation,
                        Address_PostalCode = _model.Address_PostalCode,
                        SA_ID = _model.SA_ID,                       
                        FarmerId = dtoFarmerId.Value,                        
                        Active = _model.Active,
                        ModifiedBy = 1,
                        ModifiedDate = DateTime.Now,
                    };
                    i_recs = await farmersService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {
                    dtoFarmerId = await farmersService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoFarmerId.HasValue)
                    {
                        _Toast.ShowSuccess("Client updated successfully!", "Edit Client");
                    }
                    else
                        _Toast.ShowSuccess("Client added successfully!", "Add Client");
                }

                await RefreshDataAsync();    // 1. Refresh the background grid so the user sees the new data

                if (dtoFarmerId.HasValue)
                {
                    await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                }
                else
                {
                    _model = new CreateFarmerDTO { Active = true, CreatedDate = DateTime.Now, CreatedBy = 1 };      // If it was an ADD, keep panel open and RESET for the next record                  
                    StateHasChanged();                             // StateHasChanged ensures the form fields clear out
                }            
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Saving farmer ..", false);
            }
            finally
            {
                isSaving = false;
            }
        }

        void DeleteFarmer()
        {
            //return Task.CompletedTask;
        }


    }
}
