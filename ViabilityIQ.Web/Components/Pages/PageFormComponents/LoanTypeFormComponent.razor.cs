using Microsoft.AspNetCore.Components;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class LoanTypeFormComponent
    {
        [Inject] private MasterDataService? MasterData { get; set; }

        [Parameter] public long LoanTypeId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }

        // Main working model instance bound to forms
        private LoanType loanTypeModel = new();

        // Track state variables cleanly
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (LoanTypeId == 0)
            {
                loanTypeModel = new LoanType
                {
                    LoanTypeName = string.Empty,
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
                    var existingRecord = await MasterData!.GetLoanTypeByIdAsync(LoanTypeId);
                    if (existingRecord != null)
                    {
                        loanTypeModel = existingRecord;
                        isRowActive = loanTypeModel.Active;
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
                loanTypeModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                bool executionOutcome = await MasterData!.SaveLoanTypeAsync(loanTypeModel);

                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true,
                        ClosePanel = true,
                        ClearForm = true,
                        Message = LoanTypeId == 0 ?
                         $"{loanTypeModel.LoanTypeName} added successfully" :
                         $"{loanTypeModel.LoanTypeName} updated successfully"
                    };

                    await OnSavedSuccess.InvokeAsync(saveResult);
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
