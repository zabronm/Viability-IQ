using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ClientFormComponent
    {
        //[Inject] private MasterDataService? MasterData { get; set; }
        [Inject] private IGenericDataRepository<Client>? clientRepository { get; set; }
        [Parameter] public long ClientId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }


        private Client clientModel = new();        // Main working model instance bound to forms

        private bool isProcessingData = false;               // Track state variables cleanly
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            await InitializeFormLifecycleAsync();
        }

        private async Task InitializeFormLifecycleAsync()
        {
            if (ClientId == 0)
            {
                clientModel = new()
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
                    //Province = string.Empty,
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
            else
            {
                isProcessingData = true;
                try
                {
                    //var existingRecord = await MasterData!.GetBankByIdAsync(BankId);
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
                clientModel.Active = isRowActive;

                // Fire singular service endpoint to decide Insert vs Update dynamically
                //bool executionOutcome = await MasterData!.SaveBankAsync(bankModel);

                bool executionOutcome = await clientRepository!.SaveAsync(clientModel);
                if (executionOutcome)
                {
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true
                    };

                    if (ClientId == 0)
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = false;
                        saveResult.Message = $"{clientModel.FullName} added successfully";
                    }
                    else
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = true;
                        saveResult.Message = $"{clientModel.FullName} updated successfully";
                    }

                    await OnSavedSuccess.InvokeAsync(saveResult);
                }
                else
                {
                    var saveResult = new SaveResult()
                    {
                        Success = false,
                        ClosePanel = false,
                        ClearForm = false,
                        RefreshGrid = false,
                        Message = $"Error encountered while saving client.",
                    };
                }
            }
            catch (Exception ex)
            {
                var saveResult = new SaveResult()
                {
                    Success = false,
                    RefreshGrid = false,
                    ClosePanel = false,
                    Message = $"Critical validation channel anomaly: {ex.Message}",
                };               
            }
            finally
            {
                isProcessingData = false;
                StateHasChanged();
            }
        }
    }

}
