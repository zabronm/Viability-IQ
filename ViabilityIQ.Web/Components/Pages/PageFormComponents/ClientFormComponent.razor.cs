using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ClientFormComponent
    {
        [Inject] private IGenericDataRepository<Client>? clientRepository { get; set; }
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject] private ILogger<ClientFormComponent> Logger { get; set; } = default!;

        [Parameter] public long ClientId { get; set; } = 0;

        private Client clientModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (ClientId == 0)
            {
                ResetForm();
            }
            else
            {
                isProcessingData = true;
                try
                {
                    var existingRecord = await clientRepository!.GetByIdAsync(ClientId);
                    if (existingRecord != null)
                    {
                        clientModel = existingRecord;
                        isRowActive = clientModel.Active;
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
                var isNewRecord = clientModel.ClientId == 0;
                clientModel.Active = isRowActive;

                bool executionOutcome = await clientRepository!.SaveAsync(clientModel);
                if (executionOutcome)
                {
                    var clientName = clientModel.FullName;
                    var saveResult = isNewRecord
                        ? SaveResult.SavedAndNew(
                            clientModel,
                            $"{clientName} added successfully")
                        : SaveResult.SavedAndClose(
                            clientModel,
                            $"{clientName} updated successfully");

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
                        Message = "Error encountered while saving client."
                    };
                    await OffcanvasService!.PublishResultAsync(saveResult);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Client save failed for client {ClientId}", ClientId);
                var saveResult = new SaveResult()
                {
                    Success = false,
                    ClosePanel = false,
                    Message = "The client could not be saved. Please try again."
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
            clientModel = new Client
            {
                FullName = string.Empty,
                IDNumber = string.Empty,
                GenderId = 0,
                RaceId = 0,
                SA_ID = false,
                Telephone = string.Empty,
                Mobile = string.Empty,
                Email = string.Empty,
                Address_Street = string.Empty,
                Address_Surburb = string.Empty,
                Address_CityTown = string.Empty,
                ProvinceId = 0,
                Address_Postal = string.Empty,
                Address_PostalCity = string.Empty,
                Address_PostalCode = string.Empty,
                Address_PostalLocation = string.Empty,
                Country = string.Empty,
                Remarks = string.Empty,
                Active = true
            };
            isRowActive = true;
        }
    }
}