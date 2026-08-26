using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ProductCategoryFormComponent
    {
        [Inject] private IGenericDataRepository<ProductCategory>? categoryRepository { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long ProductCategoryId { get; set; } = 0;

        private ProductCategory productCategoryModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (ProductCategoryId == 0)
            {
                productCategoryModel = new()
                {
                    ProductCategoryName = string.Empty,
                    UOM = string.Empty,
                    MarkupPercentage = 0,
                    Remarks = string.Empty,
                    Active = true
                };
                isRowActive = true;
            }
            else
            {
                isProcessingData = true;
                try
                {
                    var existingRecord = await categoryRepository!.GetByIdAsync(ProductCategoryId);
                    if (existingRecord != null)
                    {
                        productCategoryModel = existingRecord;
                        isRowActive = productCategoryModel.Active;
                    }
                }
                finally
                {
                    isProcessingData = false;
                }
            }
        }

        private async Task HandleFormSubmissionAsync()
        {
            isProcessingData = true;
            StateHasChanged();

            var saveResult = new SaveResult();

            try
            {
                productCategoryModel.Active = isRowActive;

                bool executionOutcome = await categoryRepository!.SaveAsync(productCategoryModel);
                if (executionOutcome)
                {
                    saveResult.Success = true;
                    saveResult.RefreshGrid = true;
                    saveResult.ClosePanel = true;
                    saveResult.Message = ProductCategoryId == 0
                        ? $"{productCategoryModel.ProductCategoryName} added successfully"
                        : $"{productCategoryModel.ProductCategoryName} updated successfully";
                }
                else
                {
                    saveResult.Success = false;
                    saveResult.ClosePanel = false;
                    saveResult.Message = "Error encountered while saving category.";
                }

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(saveResult);
            }
            catch (Exception ex)
            {
                saveResult.Success = false;
                saveResult.ClosePanel = false;
                saveResult.Message = $"Error: {ex.Message}";

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(saveResult);
            }
            finally
            {
                isProcessingData = false;
                StateHasChanged();
            }
        }
    }
}