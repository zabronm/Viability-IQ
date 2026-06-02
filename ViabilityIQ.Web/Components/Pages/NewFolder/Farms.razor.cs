
using BrucolWeb.Application.DTOs.Common;
using BrucolWeb.Application.DTOs.Farms;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.CommonUtilities;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Data;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class Farms
    {
        [Inject] protected FarmService? farmService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateFarmDTO> OnSave { get; set; }

        private CreateFarmDTO _model = new CreateFarmDTO();
        private List<FarmListDTO> farms = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Farm";
        private bool isSaving = false;
        private bool isLoading = false;
        private string? ButtonMessage;
        private long? dtoFarmId { get; set; }

        private int i_recs { get; set; }
        private bool isSuccess { get; set; }


        List<ZabDataTable<FarmListDTO>.ColumnDefinition<FarmListDTO>> columns = new()
        {
            new() { Title = "Farm", Value = x => x.FarmName },
            //new() { Title = "CK Number", Value = x => x.CKNumber },
            new() { Title = "Client", Value = x => x.FarmerName },
            //new() { Title = "Size(Sqm)", Value = x => x.Size },
            //new() { Title = "Location", Value = x => x.AreaLocation },
            new() { Title = "Manager", Value = x => x.ContactPerson },
            new() { Title = "Province", Value = x => x.ProvinceName },
            new() { Title = "Telephone", Value = x => x.Telephone },
            new() { Title = "Mobile", Value = x => x.Mobile },
            new() { Title = "Email", Value = x => x.Email },
            //new() { Title = "Website", Value = x => "Website .." } // placeholder
        };


        protected override async Task OnInitializedAsync()
        {
            await RefreshData();
        }

        private async Task RefreshData()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var result = await farmService!.GetListAsync();
                if (result != null)
                {
                    farms = result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED", false);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }


        async Task CreateFarm()
        {
            try
            {
                ButtonMessage = "Create Farm";
                offcanvasTitle = "Add Farm";
                dtoFarmId = null;
                _model = new CreateFarmDTO
                { Active = true, CreatedBy = 1, CreatedDate = DateTime.Now };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "Error encountered", false);
            }
        }

        void EditFarmDetails(long id) => Nav.NavigateTo($"/farmPages/edit/{id}");


        //EDIT FARM IN OFF-CANVAS
        async Task EditFarm(FarmListDTO farm)
        {
            ButtonMessage = "Update Farm";
            offcanvasTitle = "Edit Farm";
            dtoFarmId = farm.FarmId;
            _model = new CreateFarmDTO
            {
                FarmName = farm.FarmName,
                CKNumber = farm.CKNumber,
                FarmerId = farm.FarmerId,
                Size = farm.Size,
                AreaLocation = farm.AreaLocation,
                ProvinceId = farm.ProvinceId,
                ContactPerson = farm.ContactPerson,
                Telephone = farm.Telephone,
                Mobile = farm.Mobile,
                Email = farm.Email,
                Website = farm.Website,
                Remarks = farm.Remarks,
                Active = farm.Active
            };

            await offcanvas!.Show();
        }    

        private async Task HandleSubmit()
        {
            if (farmService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;

                if (dtoFarmId.HasValue)
                {
                    // UPDATE LOGIC
                    var updateModel = new UpdateFarmDTO
                    {
                        FarmId = dtoFarmId.Value,
                        FarmName = _model.FarmName,
                        FarmerId = _model.FarmerId,                                          
                        CKNumber = _model.CKNumber,
                        Size = _model.Size,
                        AreaLocation = _model.AreaLocation,
                        ProvinceId = _model.ProvinceId,
                        ContactPerson = _model.ContactPerson,
                        Telephone = _model.Telephone,
                        Mobile = _model.Mobile,
                        Email = _model.Email,
                        Website= _model.Website,
                        Remarks = _model.Remarks,
                        Active = _model.Active
                    };
                    i_recs = await farmService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {
                    // CREATE LOGIC
                    dtoFarmId = await farmService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoFarmId.HasValue)
                    {
                        _Toast.ShowSuccess("Farm updated successfully!", "Edit farm");
                    }
                    else
                    {
                        _Toast.ShowSuccess("Farm added successfully!", "Add farm");
                    }

                    await RefreshData();    // 1. Refresh the background grid so the user sees the new data

                    if (dtoFarmId.HasValue)
                    {
                        await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                    }
                    else
                    {
                        _model = new CreateFarmDTO { Active = true, CreatedDate=DateTime.Now, CreatedBy=1 };      // If it was an ADD, keep panel open and RESET for the next record                  
                        StateHasChanged();                                // StateHasChanged ensures the form fields clear out
                    }
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "Error encountered", false);
            }
            finally
            {
                isSaving = false;
            }
        }

        protected Task DeleteFarm()
        {
            return Task.CompletedTask;
        }

    }
}
