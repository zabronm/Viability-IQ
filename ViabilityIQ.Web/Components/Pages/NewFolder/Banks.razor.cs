using BrucolWeb.Application.DTOs.Banks;
using BrucolWeb.Application.Services;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class Banks
    {        
        [Inject] protected BankService? bankService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }        
        [Parameter] public EventCallback<UpdateBankDTO> OnSave { get; set; }

        private long? dtoBankId;
        private long? bankId;
        private CreateBankDTO _model = new CreateBankDTO();
        private List<BankListDTO> banks = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Bank";
        private bool isSaving = false;
        private bool isLoading = false;
        private string? ButtonMessage;
        private int i_recs { get; set; }
        private bool isSuccess { get; set; }


        List<ZabDataTable<BankListDTO>.ColumnDefinition<BankListDTO>> columns = new()
        {
            new() { Title = "Bank Name", Value = x => x.BankName },
            new() { Title = "Short Name", Value = x => x.ShortName },
            new() { Title = "Active", Value = x => x.Active ? "Yes" : "No" },
            new() { Title = "Remarks", Value = x => x.Remarks },
        };

        protected override async Task OnInitializedAsync()
        {           
                await RefreshData();
        }


        //Load data into he list here, this method can also be called to refresh the list after an Add or Edit operation
        private async Task RefreshData()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var result = await bankService!.GetListAsync();
                banks = result.ToList();
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


        async Task CreateBank()
        {
            try
            {                
                ButtonMessage = "Create Bank";
                offcanvasTitle = "Add Bank";
                dtoBankId = null;
                _model = new CreateBankDTO
                { Active = true, CreatedBy = 1, CreatedDate = DateTime.Now };
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);      //repplace with a logger service 
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create bank ..", false);
            }            
        }

        void EditBankDetails(long id) => Nav.NavigateTo($"/bankPages/edit/{id}");


        //EDIT BANK IN OFF-CANVAS
        async Task EditBank(BankListDTO bank)
        {
            try
            {                
                ButtonMessage = "Update Bank";
                offcanvasTitle = "Edit Bank";
                dtoBankId = bank.BankId;
                _model = new CreateBankDTO
                {
                    //BankId = bank.BankId, CreateBank does not have BankID, hence it will pass a null to this field
                    //This will be important in deciding if to call Create or Update in the HandleSubmit method
                    ShortName = bank.ShortName,
                    BankName = bank.BankName,
                    Remarks = bank.Remarks,
                    Active = bank.Active
                };

                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);      //replace this with proper logging
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Edit bank ..", false);
            }
            finally
            {
                // Cleanup or final actions
            }
        }

        protected Task DeleteBank()
        {
            return Task.CompletedTask;
        }
        private async Task HandleSubmit()
        {
            if (bankService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;
                if (dtoBankId.HasValue)
                {
                    // UPDATE LOGIC
                    var updateModel = new UpdateBankDTO
                    {
                        BankId = dtoBankId.Value,
                        BankName = _model.BankName,
                        ShortName = _model.ShortName,
                        Remarks = _model.Remarks,
                        Active = _model.Active
                    };
                    i_recs = await bankService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {
                    // CREATE LOGIC
                    bankId = await bankService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoBankId.HasValue)
                    {
                        _Toast.ShowInfo("Bank updated successfully!", "Edit Bank");
                    }
                    else
                    {
                        _Toast.ShowInfo("Bank created successfully!", "Add Bank");
                    }   
                                        
                    await RefreshData();    // 1. Refresh the background grid so the user sees the new data

                    if (dtoBankId.HasValue)
                    {
                        await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                    }
                    else
                    {                        
                        _model = new CreateBankDTO { Active = true };      // If it was an ADD, keep panel open and RESET for the next record                  
                        StateHasChanged();                                // StateHasChanged ensures the form fields clear out
                    }
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Saving bank ..",false);
            }
            finally
            {
                isSaving = false;
            }
        }
               

    }
}
