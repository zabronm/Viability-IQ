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
                    ClientName = string.Empty,
                    IDNumber = string.Empty,
                    Gender = 0,
                    Race = 0,
                    SA_ID = false,
                    Telephone = string.Empty,
                    Mobile = string.Empty,
                    Email = string.Empty,
                    Street_Address = string.Empty,
                    Suburb = string.Empty,
                    CityTown = string.Empty,
                    ProvinceId = 0,
                    //Province = string.Empty,
                    Postal_Address = string.Empty,
                    Postal_City = string.Empty,
                    PostalCode = string.Empty,
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
                        saveResult.Message = $"{clientModel.ClientName} added successfully";
                    }
                    else
                    {
                        saveResult.ClearForm = true;
                        saveResult.ClosePanel = true;
                        saveResult.Message = $"{clientModel.ClientName} updated successfully";
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
