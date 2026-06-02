using BrucolWeb.Application.DTOs.Crops;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Data;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class Crops
    {
        [Inject] protected CropService? cropService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateCropDTO> OnSave { get; set; }

        private CreateCropDTO _model = new CreateCropDTO();
        private List<CropListDTO> crops = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Crop";
        private bool isSaving = false;
        private string? ButtonMessage;
        private bool isLoading = false;
        private int? i_recs;
        private long? dtoCropId { get; set; } = null;

        List<ZabDataTable<CropListDTO>.ColumnDefinition<CropListDTO>> columns = new()
        {
            new() { Title = "Crop", Value = x => x.CropName },
            new() { Title = "Botany Name", Value = x => x.BotanicalName },
            new() { Title = "Category", Value = x => x.CategoryName },
            new() {Title  = "Active", Value= x => x.Active==true? "Yes":"No" },
            new() { Title = "Remarks", Value = x => x.Remarks },
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

                var result = await cropService!.GetListAsync();
                crops = result.ToList();
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


        async Task CreateCrop()
        {
            try
            {
                ButtonMessage = "Create Crop";
                offcanvasTitle = "Add Crop";
                dtoCropId = null;
                _model = new CreateCropDTO
                { Active = true, CreatedBy = 1, CreatedDate = DateTime.Now };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create crop", false);
            }
        }

        void EditCropDetails(long id) => Nav.NavigateTo($"/farmPages/edit/{id}");


        //EDIT FARM IN OFF-CANVAS
        async Task EditCrop(CropListDTO crop)
        {
            ButtonMessage = "Update Crop";
            offcanvasTitle = "Edit Crop";
            dtoCropId = crop.CropId;
            _model = new CreateCropDTO
            {
                 CropName = crop.CropName,
                  BotanicalName = crop.BotanicalName,                                
                Remarks = crop.Remarks,
                Active = crop.Active

            };
            await offcanvas!.Show();
        }


        void DeleteCrop()
        {
            //Task.CompletedTask;
        }
        private async Task HandleSubmit()
        {
            if (cropService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;

                if (dtoCropId.HasValue)
                {                   
                    var updateModel = new UpdateCropDTO
                    {
                        BotanicalName = _model.BotanicalName,                         
                        CropId = dtoCropId.Value,
                        CropName = _model.CropName,
                        Active = _model.Active,
                        ModifiedBy = 1,
                        ModifiedDate = DateTime.Now,
                    };
                    i_recs = await cropService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {                   
                    dtoCropId = await cropService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoCropId.HasValue)
                    {
                        _Toast.ShowSuccess("Crop updated successfully!", "Edit Crop");
                    }
                    else
                    {
                        _Toast.ShowSuccess("Crop added successfully!", "Add Crop");
                    }

                    await RefreshDataAsync();    // 1. Refresh the background grid so the user sees the new data

                    if (dtoCropId.HasValue)
                    {
                        await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                    }
                    else
                    {
                        _model = new CreateCropDTO { Active = true, CreatedDate = DateTime.Now, CreatedBy = 1 };      // If it was an ADD, keep panel open and RESET for the next record                  
                        StateHasChanged();                             // StateHasChanged ensures the form fields clear out
                    }
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Saving crop ..", false);
            }
            finally
            {
                isSaving = false;
            }
        }

    }
}
