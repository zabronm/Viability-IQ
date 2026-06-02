using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ProductServiceFormComponent
    {
        //[Inject] private MasterDataService? MasterData { get; set; }
        [Inject] private IGenericDataRepository<ProductService>? productServiceRepository { get; set; }
        [Parameter] public long ProductServiceId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }

       
        private ProductService productServiceModel = new();        // Main working model instance bound to forms
       
        private bool isProcessingData = false;               // Track state variables cleanly
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
                    //var existingRecord = await MasterData!.GetBankByIdAsync(BankId);
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

            try
            {
                productServiceModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                //bool executionOutcome = await MasterData!.SaveBankAsync(bankModel);

                bool executionOutcome = await productServiceRepository!.SaveAsync(productServiceModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true
                    };

                    if (ProductServiceId == 0)
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = false;
                        saveResult.Message = $"{productServiceModel.ProductName} added successfully";
                    }
                    else
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = true;
                        saveResult.Message = $"{productServiceModel.ProductName} updated successfully";
                    }

                    await OnSavedSuccess.InvokeAsync(saveResult);
                }
                else
                {
                    var saveResult = new SaveResult()
                    {
                        Success = false,
                        ClosePanel = false,
                        ClearForm = false,
                        RefreshGrid = false,
                        Message = $"Error encountered while saving category.",
                    }; 
                }
            }
            catch (Exception ex)
            {
                var saveResult = new SaveResult()
                {
                    Success = false,
                    RefreshGrid = false,
                    ClosePanel = false,
                    Message = $"Critical validation channel anomaly: {ex.Message}",
                };               
            }
            finally
            {
                isProcessingData = false;
                StateHasChanged();
            }
        }
    }

}
