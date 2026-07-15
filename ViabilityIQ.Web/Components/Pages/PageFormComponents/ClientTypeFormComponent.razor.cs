using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ClientTypeFormComponent
    {
        [Inject] private IGenericDataRepository< ClientType> sectorRepository { get; set; } = default!;
        [Parameter] public long ClientTypeId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }

        private  ClientType clientTypeModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            if (ClientTypeId != 0)
            {
                isProcessingData = true;
                try
                {
                    var record = await sectorRepository.GetByIdAsync(ClientTypeId);
                    if (record != null)
                    {
                        clientTypeModel = record;
                        isRowActive = clientTypeModel.Active;
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
                clientTypeModel = new  ClientType();
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
                clientTypeModel.Active = isRowActive;

                // FIX: Invoking the unified data stream 'SaveAsync' as defined in GenericDataRepository
                bool operationSuccess = await sectorRepository.SaveAsync(clientTypeModel);

                if (operationSuccess)
                {
                    finalResult.Success = true;                    
                    finalResult.ClosePanel = true;
                    finalResult.Message = ClientTypeId == 0
                        ? "New client type registered successfully."
                        : "Client type modified successfully.";
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
                finalResult.Message = $"Error encountered: {ex.Message}";

                await OnSavedSuccess.InvokeAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }
    }
}