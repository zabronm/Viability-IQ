using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ProductCategoryFormComponent
    {
        //[Inject] private MasterDataService? MasterData { get; set; }
        [Inject] private IGenericDataRepository<ProductCategory>? categoryRepository { get; set; }
        [Parameter] public long ProductCategoryId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }

       
        private ProductCategory productCategoryModel = new();        // Main working model instance bound to forms
       
        private bool isProcessingData = false;               // Track state variables cleanly
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
                    //var existingRecord = await MasterData!.GetBankByIdAsync(BankId);
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

            try
            {
                productCategoryModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                //bool executionOutcome = await MasterData!.SaveBankAsync(bankModel);

                bool executionOutcome = await categoryRepository!.SaveAsync(productCategoryModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true
                    };

                    if (ProductCategoryId == 0)
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = false;                        
                        saveResult.Message = $"{productCategoryModel.ProductCategoryName} added successfully";
                    }
                    else
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = true;                       
                        saveResult.Message = $"{productCategoryModel.ProductCategoryName} updated successfully";
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
