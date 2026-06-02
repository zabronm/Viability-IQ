using BrucolWeb.Application.DTOs.Applications;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Interfaces;
using BrucolWeb.Web.Components.Common;
using BrucolWeb.Web.Services;
using Microsoft.AspNetCore.Components;

namespace BrucolWeb.Web.Components.Pages
{
    public partial class LoanApplications
    {
        [Inject] protected IApplicationSetupRepository? applicationSetupService { get; set; }
        [Inject] protected ZabSessionService? ZabSession { get; set; }
        private long? dtoAplicationId;
        private long? applicationId;
        private CreateLoanApplicationDTO _model = new();
        private List<LoanApplicationListDTO> loanApplications = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Loan Application";
        private bool isSaving = false;
        private bool isLoading = false;
        private string? ButtonMessage;
        private int i_recs { get; set; }
        private bool isSuccess { get; set; }
        private long rec_count { get; set; }



        protected List<LoanApplicationListDTO> LoanApplicationsList { get; set; } = new();
        protected List<ZabDataTableAdvanced<LoanApplicationListDTO>.ColumnDefinition<LoanApplicationListDTO>> TableColumns { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            var result = await LoanApplicationService.GetListAsync();
            LoanApplicationsList = result.ToList();

            TableColumns = new()
        {
            new() {  Title = "File No",       Value = x => x.FileNumber, IsLink = true, OnClick = x => OpenApplication(x)            },
            new() {  Title = "Loan Type",       Value = x => x.LoanType  },
            new() {  Title = "Client",       Value = x => x.FarmerName, IsLink = true, OnClick = x => OpenApplication(x)            },
            new() {  Title = "Farm",       Value = x => x.FarmName, IsLink = true, OnClick = x => OpenApplication(x)            },
            new() {  Title = "Loan Date",       Value = x => x.ApplicationDate, FormatString = "dd MMM yyyy"            },
            new() {  Title = "Season",       Value = x => x.SeasonName            },
            new() {  Title = "Amount Reqd",   Value = x => x.AmountRequested, Formatter = v =>  $"R {Convert.ToDecimal(v):N2}",  CssClass = "text-end" }
        };
        }


        private async Task RefreshData()
        {
            await LoadDataAsync();
        }

        async Task LoadDataAsync()
        {
            try
            {
                isLoading = true;
                StateHasChanged();
                var result = await LoanApplicationService.GetListAsync();
                LoanApplicationsList = result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Loading data ..", false);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }


        async Task CreateApplication()
        {
            try
            {
                ButtonMessage = "Create Loan Application";
                offcanvasTitle = "Add Loan Application";
                dtoAplicationId = null;
                _model = new CreateLoanApplicationDTO
                { Active = true, CreatedBy = 1, CreatedDate = DateTime.Now };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);      //repplace with a logger service 
                _Toast!.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create loanApplication ..", false);
            }
        }


        async Task EditApplication(LoanApplicationListDTO dto)
        {
            try
            {
                ButtonMessage = "Update Loan Application";
                offcanvasTitle = "Edit Loan Application";
                dtoAplicationId = dto.ApplicationId;
                _model = new CreateLoanApplicationDTO
                {
                    //This will be important in deciding if to call Create or Update in the HandleSubmit method
                    AmountRequested = dto.AmountRequested,
                    ApplicationDate = dto.ApplicationDate,
                    SeasonId = dto.SeasonId,
                    BankId = dto.BankId,
                    FarmerId = dto.FarmerId,
                    FarmId = dto.FarmId,
                    LoanTypeId = dto.LoanTypeId,
                    Remarks = dto.Remarks,
                    Active = dto.Active,
                };

                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);      //replace this with proper logging
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Edit loanApplication ..", false);
            }
            finally
            {
                // Cleanup or final actions
            }
        }


        protected async Task DeleteApplication(LoanApplicationListDTO row)
        {            
            LoanApplicationsList.Remove(row);
            await InvokeAsync(StateHasChanged);
        }


        protected void HandleEditButtonClick(LoanApplicationListDTO row)
        {            
            Nav.NavigateTo($"/applications_details/{row.ApplicationId}/compliance");
        }

        private async Task HandleSubmit()
        {
            if (LoanApplicationService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;
                if (dtoAplicationId.HasValue)
                {
                    // UPDATE LOGIC
                    var updateModel = new UpdateLoanApplicationDTO
                    {                        
                        ApplicationId = dtoAplicationId.Value,
                        SeasonId = _model.SeasonId,
                        AmountRequested = _model.AmountRequested,
                        ApplicationDate = _model.ApplicationDate,
                        BankId = _model.BankId,
                        FarmerId = _model.FarmerId,
                        ModifieDate = DateTime.Now,
                        ModifiedBy = 1,     //freplace with user Id in LIVE
                        Remarks = _model.Remarks,
                        Active = _model.Active
                    };
                    i_recs = await LoanApplicationService.UpdateAsync(updateModel);
                    isSuccess = true;

                    await RefreshData();    // 1. Refresh the background grid so the user sees the new data
                    _Toast.ShowSuccess("Application updated successfully!", "Edit Loan Application");
                    await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                   
                }
                else
                {
                    // CREATE LOGIC

                    dtoAplicationId = await LoanApplicationService.CreateAsync(_model);

                    if (dtoAplicationId.HasValue)
                    {
                        rec_count = await applicationSetupService!.InitializeApplicationPhasesAsync(dtoAplicationId.Value);
                        if (rec_count > 0)
                        {
                            rec_count = await applicationSetupService!.InitializeApplicationDocumentsAsync(dtoAplicationId.Value);
                        }
                    }
                    else
                    {
                        new Exception("Error encountered during application initialise.");
                    }                 
                    isSuccess = true;

                    await RefreshData();    // 1. Refresh the background grid so the user sees the new data
                    _Toast.ShowSuccess("Application added successfully!", "Add Loan Application");
                    _model = new CreateLoanApplicationDTO { Active = true, CreatedDate = DateTime.Now, CreatedBy = 1 };      // If it was an ADD, keep panel open and RESET for the next record                  

                    dtoAplicationId = null;                           //Very important, set this to null for a new application
                    StateHasChanged();                               // StateHasChanged ensures the form fields clear out
                }                
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "Error encountered", false);
            }
            finally
            {
                isSaving = false;
            }
        }



        async Task OpenApplication(LoanApplicationListDTO row)
        {
            offcanvasTitle = "Edit Loan Application";
            await EditApplication(row);
            //Nav.NavigateTo($"/applications/{row.ApplicationId}/compliance");
            //return Task.CompletedTask;
        }


        async Task EditFarmer(long farmerId)
        {         
            //return Task.CompletedTask;
        }

        async Task EditFarm(long farmId)
        {
            //return Task.CompletedTask;
        }
    }
}
