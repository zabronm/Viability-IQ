using Microsoft.AspNetCore.Components;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class BankFormComponent
    {       
        [Inject] private MasterDataService? MasterData { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long BankId { get; set; } = 0;       

        // Main working model instance bound to forms
        private Bank bankModel = new();

        // Track state variables cleanly
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (BankId == 0)
            {
                bankModel = new Bank
                {
                    BankName = string.Empty,
                    ShortName = string.Empty,
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
                    var existingRecord = await MasterData!.GetBankByIdAsync(BankId);
                    if (existingRecord != null)
                    {
                        bankModel = existingRecord;
                        isRowActive = bankModel.Active;
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
                bankModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                bool executionOutcome = await MasterData!.SaveBankAsync(bankModel);

                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true,
                        ClosePanel = true,
                        ClearForm = true,
                        Message = BankId == 0 ?
                         $"{bankModel.BankName} added successfully" :
                         $"{bankModel.BankName} updated successfully"
                    };

                    //await OnSavedSuccess.InvokeAsync(saveResult);
                    await OffcanvasService!.PublishResultAsync(saveResult);
                }
                else
                {
                    var saveResult = new SaveResult()
                    {
                        Success = false,
                        ClosePanel = false,
                        Message = "Error encountered while saving bank."
                    };
                    await OffcanvasService!.PublishResultAsync(saveResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical validation channel anomaly: {ex.Message}");
            }
            finally
            {
                isProcessingData = false;
                StateHasChanged();
            }
        }
    }

}
