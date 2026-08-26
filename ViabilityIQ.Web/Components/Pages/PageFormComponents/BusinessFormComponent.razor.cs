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
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

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

                bool executionOutcome = await businessRepository!.SaveAsync(businessModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true,
                        ClosePanel = true,  // ✅ Always close on success
                        Message = BusinessId == 0
                            ? $"{businessModel.BusinessName} added successfully"
                            : $"{businessModel.BusinessName} updated successfully"
                    };

                    // ✅ Use service to publish result
                    await OffcanvasService!.PublishResultAsync(saveResult);
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
                var saveResult = new SaveResult()
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Error: {ex.Message}"
                };
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