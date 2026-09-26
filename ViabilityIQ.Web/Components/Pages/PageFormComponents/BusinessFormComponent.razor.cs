using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class BusinessFormComponent
    {
        [Inject] private IGenericDataRepository<Business>? businessRepository { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject] private ILogger<BusinessFormComponent> Logger { get; set; } = default!;

        [Parameter] public long BusinessId { get; set; } = 0;

        private Business businessModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (BusinessId == 0)
            {
                ResetForm();
            }
            else
            {
                isProcessingData = true;
                try
                {
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
                var isNewRecord = businessModel.BusinessId == 0;
                businessModel.Active = isRowActive;

                bool executionOutcome = await businessRepository!.SaveAsync(businessModel);
                if (executionOutcome)
                {
                    var businessName = businessModel.BusinessName;
                    var saveResult = isNewRecord
                        ? SaveResult.SavedAndNew(
                            businessModel,
                            $"{businessName} added successfully")
                        : SaveResult.SavedAndClose(
                            businessModel,
                            $"{businessName} updated successfully");

                    await OffcanvasService!.PublishResultAsync(saveResult);

                    if (saveResult.ClearForm)
                    {
                        ResetForm();
                    }
                }
                else
                {
                    var saveResult = new SaveResult()
                    {
                        Success = false,
                        ClosePanel = false,
                        Message = "Error encountered while saving business."
                    };
                    await OffcanvasService!.PublishResultAsync(saveResult);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Business save failed for business {BusinessId}", BusinessId);
                var saveResult = new SaveResult()
                {
                    Success = false,
                    ClosePanel = false,
                    Message = "The business could not be saved. Please try again."
                };
                await OffcanvasService!.PublishResultAsync(saveResult);
            }
            finally
            {
                isProcessingData = false;
                StateHasChanged();
            }
        }

        private void ResetForm()
        {
            businessModel = new Business
            {
                Active = true
            };
            isRowActive = true;
        }

        private Task CancelAsync() => OffcanvasService!.CloseAsync();
    }
}