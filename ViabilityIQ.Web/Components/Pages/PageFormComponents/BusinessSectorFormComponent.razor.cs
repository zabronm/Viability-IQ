using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class BusinessSectorFormComponent
    {
        [Inject] private IGenericDataRepository<BusinessSector> sectorRepository { get; set; } = default!;
        [Parameter] public long BusinessSectorId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }

        private BusinessSector sectorModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            if (BusinessSectorId != 0)
            {
                isProcessingData = true;
                try
                {
                    var record = await sectorRepository.GetByIdAsync(BusinessSectorId);
                    if (record != null)
                    {
                        sectorModel = record;
                        isRowActive = sectorModel.Active;
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
                sectorModel = new BusinessSector();
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
                sectorModel.Active = isRowActive;

                // FIX: Invoking the unified data stream 'SaveAsync' as defined in GenericDataRepository
                bool operationSuccess = await sectorRepository.SaveAsync(sectorModel);

                if (operationSuccess)
                {
                    finalResult.Success = true;                    
                    finalResult.ClosePanel = true;
                    finalResult.Message = BusinessSectorId == 0
                        ? "New business sector classification logged successfully."
                        : "Business sector definitions modified successfully.";
                }
                else
                {
                    throw new Exception("Error encountered while committing. Please retry.");
                }

                await OnSavedSuccess.InvokeAsync(finalResult);
            }
            catch (Exception ex)
            {
                finalResult.Success = false;               
                finalResult.ClosePanel = false;
                finalResult.Message = $"Pipeline Transaction Fault Intercepted: {ex.Message}";

                await OnSavedSuccess.InvokeAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }
    }
}