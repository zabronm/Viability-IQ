using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ProvinceFormComponent
    {
        [Inject] private IGenericDataRepository<Province>? provinceRepository { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;

        [Parameter] public long ProvinceId { get; set; } = 0;

        private Province provinceModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (ProvinceId == 0)
            {
                provinceModel = new()
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
                    var existingRecord = await provinceRepository!.GetByIdAsync(ProvinceId);
                    if (existingRecord != null)
                    {
                        provinceModel = existingRecord;
                        isRowActive = provinceModel.Active;
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
                provinceModel.Active = isRowActive;

                bool executionOutcome = await provinceRepository!.SaveAsync(provinceModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true,
                        ClosePanel = true,
                        Message = ProvinceId == 0
                            ? $"{provinceModel.ProvinceName} added successfully"
                            : $"{provinceModel.ProvinceName} updated successfully"
                    };

                    await OffcanvasService!.PublishResultAsync(saveResult);
                }
                else
                {
                    var saveResult = new SaveResult()
                    {
                        Success = false,
                        ClosePanel = false,
                        Message = "Error encountered while saving province."
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
