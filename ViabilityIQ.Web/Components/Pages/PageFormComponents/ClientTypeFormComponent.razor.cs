using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ClientTypeFormComponent
    {
        [Inject] private IGenericDataRepository<ClientType> clientTypeRepository { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long ClientTypeId { get; set; } = 0;

        private ClientType clientTypeModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            if (ClientTypeId != 0)
            {
                isProcessingData = true;
                try
                {
                    var record = await clientTypeRepository.GetByIdAsync(ClientTypeId);
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
                clientTypeModel = new ClientType();
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

                // ✅ Invoking the unified data stream 'SaveAsync' as defined in GenericDataRepository
                bool operationSuccess = await clientTypeRepository.SaveAsync(clientTypeModel);

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

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            catch (Exception ex)
            {
                finalResult.Success = false;
                finalResult.ClosePanel = false;
                finalResult.Message = $"Error encountered: {ex.Message}";

                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }
    }
}