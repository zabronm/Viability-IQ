using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class BusinessFormComponent
    {
        //[Inject] private MasterDataService? MasterData { get; set; }
        [Inject] private IGenericDataRepository<Business>? businessRepository { get; set; }
        [Parameter] public long BusinessId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }


        private Business businessModel = new();        // Main working model instance bound to forms

        private bool isProcessingData = false;               // Track state variables cleanly
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (BusinessId == 0)
            {
                businessModel = new()
                {                   
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
                    var existingRecord = await businessRepository!.GetByIdAsync(BusinessId);
                    if (existingRecord != null)
                    {
                        businessModel = existingRecord;
                        isRowActive = businessModel.Active;
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
                businessModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                //bool executionOutcome = await MasterData!.SaveBankAsync(bankModel);

                bool executionOutcome = await businessRepository!.SaveAsync(businessModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true
                    };

                    if (BusinessId == 0)
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = false;
                        saveResult.Message = $"{businessModel.BusinessName} added successfully";
                    }
                    else
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = true;
                        saveResult.Message = $"{businessModel.BusinessName} updated successfully";
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
                        Message = $"Error encountered while saving business.",
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
