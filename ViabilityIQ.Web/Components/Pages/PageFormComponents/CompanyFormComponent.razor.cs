using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class CompanyFormComponent
    {
        [Inject] private IGenericDataRepository<Company>? companyRepository { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long CompanyId { get; set; } = 0;

        private Company companyModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (CompanyId == 0)
            {
                companyModel = new()
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
                    var existingRecord = await companyRepository!.GetByIdAsync(CompanyId);
                    if (existingRecord != null)
                    {
                        companyModel = existingRecord;
                        isRowActive = companyModel.Active;
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
                companyModel.Active = isRowActive;

                bool executionOutcome = await companyRepository!.SaveAsync(companyModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true,
                        ClosePanel = true,
                        Message = CompanyId == 0
                            ? $"{companyModel.CompanyName} added successfully"
                            : $"{companyModel.CompanyName} updated successfully"
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
                        Message = "Error encountered while saving company.",
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
                    Message = $"Critical error: {ex.Message}",
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