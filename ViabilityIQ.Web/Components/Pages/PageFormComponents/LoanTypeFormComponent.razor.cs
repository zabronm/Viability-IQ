using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class LoanTypeFormComponent
    {
        [Inject] private MasterDataService? MasterData { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS
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

            var saveResult = new SaveResult();

            try
            {
                loanTypeModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                bool executionOutcome = await MasterData!.SaveLoanTypeAsync(loanTypeModel);

                if (executionOutcome)
                {
                    saveResult.Success = true;
                    saveResult.RefreshGrid = true;
                    saveResult.ClosePanel = true;
                    saveResult.Message = LoanTypeId == 0
                        ? $"{loanTypeModel.LoanTypeName} added successfully"
                        : $"{loanTypeModel.LoanTypeName} updated successfully";
                }
                else
                {
                    saveResult.Success = false;
                    saveResult.ClosePanel = false;
                    saveResult.Message = "Error encountered while saving loan type.";
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
