using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class IncomeTypeFormComponent
    {
        [Inject] private IGenericDataRepository<IncomeType> incomeTypeRepository { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long IncomeTypeId { get; set; } = 0;

        private IncomeType incomeTypeModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (IncomeTypeId != 0)
            {
                isProcessingData = true;
                try
                {
                    var record = await incomeTypeRepository.GetByIdAsync(IncomeTypeId);
                    if (record != null)
                    {
                        incomeTypeModel = record;
                        isRowActive = incomeTypeModel.Active;
                    }
                }
                finally
                {
                    isProcessingData = false;
                }
            }
            else
            {
                // Initialize fresh tracking footprint for new additions
                incomeTypeModel = new IncomeType();
                isRowActive = true;
            }
        }

        protected async Task HandleFormSubmissionAsync()
        {
            isProcessingData = true;
            var finalResult = new SaveResult();

            try
            {
                // Synchronize toggle visual states straight to model metadata properties
                incomeTypeModel.Active = isRowActive;

                // FIX: Invoking the unified data stream 'SaveAsync' as defined in GenericDataRepository
                bool operationSuccess = await incomeTypeRepository.SaveAsync(incomeTypeModel);

                if (operationSuccess)
                {
                    finalResult.Success = true;
                    finalResult.ClosePanel = true;
                    finalResult.Message = IncomeTypeId == 0
                        ? "New income type registered successfully."
                        : "Income type modified successfully.";
                }
                else
                {
                    throw new Exception("Error encountered while committing. Please retry.");
                }

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            catch (Exception ex)
            {
                finalResult.Success = false;
                finalResult.ClosePanel = false;
                finalResult.Message = $"Error encountered: {ex.Message}";

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }
    }
}