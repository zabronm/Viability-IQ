using BrucolWeb.Application.DTOs.CropCategories;
using BrucolWeb.Application.DTOs.Crops;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;



namespace BrucolWeb.Web.Components.Pages
{
    public partial class CropCategory
    {       
        [Inject] protected CropCategoryService? cropCategoryService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateCropCategoryDTO> OnSave { get; set; }

        private CreateCropCategoryDTO _model = new CreateCropCategoryDTO();
        private List<CropCategoryListDTO> cropCategorys = new();
        private ZabOffCanvas? offcanvas;

        private string offcanvasTitle = "Add Crop Category";
        private bool isSaving = false;
        private string? ButtonMessage;
        private long? dtoCropCategoryId { get; set; } = null;
        private bool isLoading { get; set; } = false;
        private int? i_recs { get; set; }
        private long? dtoCrpCategoryId { get; set; } = null;


        List<ZabDataTable<CropCategoryListDTO>.ColumnDefinition<CropCategoryListDTO>> columns = new()
        {
            new() { Title = "Crop Category", Value = x => x.CategoryName},                     
            new() { Title = "Remarks", Value = x => x.Remarks },
            new() { Title = "Active", Value = x => x.Active ? "Yes" : "No" },
        };

        protected override async Task OnInitializedAsync()
        {
            await RefreshDataAsync();
        }

        //Load data into he list here, this method can also be called to refresh the list after an Add or Edit operation
        private async Task RefreshDataAsync()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var result = await cropCategoryService!.GetListAsync();
                cropCategorys = result.ToList();
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


        async Task CreateCropCategory()
        {
            try
            {
                ButtonMessage = "Create Crop Category";
                offcanvasTitle = "Add Crop Category";
                dtoCropCategoryId = null;
                _model = new CreateCropCategoryDTO
                { Active = true, CreatedBy = 1, CreatedDate = DateTime.Now };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create cropCategory", false);
            }
        }

        void EditCropDetails(long id) => Nav.NavigateTo($"/farmPages/edit/{id}");

        void EditCropCategory()
        {
            //
        }
        //EDIT FARM IN OFF-CANVAS
        async Task EditCropCategory(CropCategoryListDTO cropCategory)
        {
            ButtonMessage = "Update Crop Category";
            offcanvasTitle = "Edit Crop Category";
            dtoCropCategoryId = cropCategory.CropCategoryId;
            _model = new CreateCropCategoryDTO
            {
                CategoryName = cropCategory.CategoryName,
                Remarks = cropCategory.Remarks,
                Active = cropCategory.Active
            };
            await offcanvas!.Show();
        }


        void  DeleteCropCategory()
        {
            //return Task.CompletedTask;
        }


        private async Task HandleSubmit()
        {
            if (cropCategoryService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;

                if (dtoCropCategoryId.HasValue)
                {
                    var updateModel = new UpdateCropCategoryDTO
                    {
                        CategoryName = _model.CategoryName,
                        CropCategoryId = dtoCropCategoryId.Value,
                        Remarks = _model.Remarks,
                        Active = _model.Active,
                        ModifiedBy = 1,
                        ModifiedDate = DateTime.Now,
                    };
                    i_recs = await cropCategoryService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {
                    dtoCropCategoryId = await cropCategoryService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoCropCategoryId.HasValue)
                    {
                        _Toast.ShowSuccess("Crop category updated successfully!", "Edit Crop Category");
                    }
                    else
                    {
                        _Toast.ShowSuccess("Crop category added successfully!", "Add Crop Category");
                    }

                    await RefreshDataAsync();    // 1. Refresh the background grid so the user sees the new data

                    if (dtoCropCategoryId.HasValue)
                    {
                        await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                    }
                    else
                    {
                        _model = new CreateCropCategoryDTO { Active = true, CreatedDate = DateTime.Now, CreatedBy = 1 };      // If it was an ADD, keep panel open and RESET for the next record                  
                        StateHasChanged();                          // StateHasChanged ensures the form fields clear out
                    }
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Saving Crop Category ..", false);
            }
            finally
            {
                isSaving = false;
            }
        }

    }
}
