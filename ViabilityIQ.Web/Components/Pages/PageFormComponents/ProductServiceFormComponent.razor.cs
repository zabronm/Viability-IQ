using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ProductServiceFormComponent
    {
        [Inject] private IGenericDataRepository<ProductService>? productServiceRepository { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long ProductServiceId { get; set; } = 0;

        private ProductService productServiceModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (ProductServiceId == 0)
            {
                productServiceModel = new()
                {
                    ProductName = string.Empty,
                    OtherName = string.Empty,
                    ProductCategoryId = 0,
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
                    var existingRecord = await productServiceRepository!.GetByIdAsync(ProductServiceId);
                    if (existingRecord != null)
                    {
                        productServiceModel = existingRecord;
                        isRowActive = productServiceModel.Active;
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
                productServiceModel.Active = isRowActive;

                bool executionOutcome = await productServiceRepository!.SaveAsync(productServiceModel);
                if (executionOutcome)
                {
                    saveResult.Success = true;
                    saveResult.RefreshGrid = true;
                    saveResult.ClosePanel = true;
                    saveResult.Message = ProductServiceId == 0
                        ? $"{productServiceModel.ProductName} added successfully"
                        : $"{productServiceModel.ProductName} updated successfully";
                }
                else
                {
                    saveResult.Success = false;
                    saveResult.ClosePanel = false;
                    saveResult.Message = "Error encountered while saving product/service.";
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