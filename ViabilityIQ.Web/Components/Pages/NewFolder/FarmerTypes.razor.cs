using BrucolWeb.Application.DTOs.Banks;
using BrucolWeb.Application.DTOs.FarmerTypes;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class FarmerTypes
    {
        [Inject] protected FarmerTypeService? farmerTypeService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateFarmerTypeDto> OnSave { get; set; }

        private CreateFarmerTypeDto _model = new CreateFarmerTypeDto();
        private List<FarmerTypeListDto> farmerTypes = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Client Category";

        private int i_recs { get; set; }
        private long farmerTypeId { get; set; }
        private long? dtoFarmTypeId { get; set; } = null;
        private bool isLoading = false;
        private bool isSaving = false;
        private string? ButtonMessage;

        List<ZabDataTable_Docs<FarmerTypeListDto>.ColumnDefinition<FarmerTypeListDto>> columns = new()
        {
            new() { Title = "Client Type", Value = x => x.FarmerType         },
            new() { Title = "Remarks", Value = x => x.Remarks },
            new() { Title = "Active", Value = x => x.Active==true? "Yes":"No"},
        };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await RefreshData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
        }

        //Load data into he list here, this method can also be called to refresh the list after an Add or Edit operation
        private async Task RefreshData()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var result = await farmerTypeService!.GetListAsync();
                farmerTypes = result.ToList();
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


        async Task CreateFarmerType()
        {
            try
            {
                ButtonMessage = "Create Client Category";
                offcanvasTitle = "Add Client Category";
                dtoFarmTypeId = null;
                _model = new CreateFarmerTypeDto()
                {
                    Active = true,
                    CreatedBy = 1,          //replace with actual user id from the auth context
                    CreatedDate = DateTime.Now
                };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);      //repplace with a logger service
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create farmer type ..", false);
            }
        }

        async Task CreateFileCheckList()
        {
            try
            {
                
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);      //repplace with a logger service
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create File CheckList ..", false);
            }
        }


        void EditFarmerTypeDetails(long id) => Nav.NavigateTo($"/farmerTypePages/edit/{id}");

        //EDIT FARMER TYPE IN OFF-CANVAS
        async Task EditFarmerType(FarmerTypeListDto farmerType)
        {
            try
            {
                ButtonMessage = "Update Client Category";
                offcanvasTitle = "Edit Client Category";
                dtoFarmTypeId = farmerType.FarmerTypeId;
                _model = new CreateFarmerTypeDto
                {
                    //FarmerTypeId = farmerType.FarmerTypeId, CreateFarmerType does not have FarmerTypeId, hence it will pass a null to this field
                    //This will be important in deciding if to call Create or Update in the HandleSubmit method                    
                    FarmerType = farmerType.FarmerType,
                    Remarks = farmerType.Remarks,
                    Active = farmerType.Active,
                    CreatedDate = farmerType.ModifiedDate,
                    CreatedBy = 1,                                      // farmerType.ModifiedBy,
                };

                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);      //replace this with proper logging
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Edit farmet type ..", false);
            }
            finally
            {
                // Cleanup or final actions
            }
        }

        protected Task DeleteFarmerType()
        {
            return Task.CompletedTask;
        }
        private async Task HandleSubmit()
        {
            if (farmerTypeService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;
                if (dtoFarmTypeId.HasValue)
                {
                    // UPDATE LOGIC
                    var updateModel = new UpdateFarmerTypeDto
                    {
                        FarmerTypeId = dtoFarmTypeId.Value,
                        FarmerType = _model.FarmerType,
                        Remarks = _model.Remarks,
                        Active = _model.Active,
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = 1,                         //Replace with actual user from Auth
                    };

                    i_recs = await farmerTypeService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {
                    // CREATE LOGIC
                    farmerTypeId = await farmerTypeService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoFarmTypeId.HasValue)
                    {
                        _Toast.ShowInfo("Client Type updated successfully!", "Edit Client Category");
                    }
                    else
                    {
                        _Toast.ShowInfo("Client Type created successfully!", "Add Client Category");
                    }

                    await RefreshData();    // 1. Refresh the background grid so the user sees the new data

                    if (dtoFarmTypeId.HasValue)
                    {
                        await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                    }
                    else
                    {
                        _model = new CreateFarmerTypeDto { Active = true };      // If it was an ADD, keep panel open and RESET for the next record                  
                        StateHasChanged();                                // StateHasChanged ensures the form fields clear out
                    }
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Saving farmer category ..", false);
            }
            finally
            {
                isSaving = false;
            }
        }

    }
}
