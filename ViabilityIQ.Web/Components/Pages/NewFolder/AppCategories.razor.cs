using BrucolWeb.Application.DTOs.ApplicationCategory;
using BrucolWeb.Application.DTOs.Banks;
using BrucolWeb.Application.Services;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Data;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class AppCategories
    {
        [Inject] protected ApplicationCategoryService? applicationCategoryService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateApplicationCategoryDTO> OnSave { get; set; }

        private CreateApplicationCategoryDTO _model = new CreateApplicationCategoryDTO();
        private List<ApplicationCategoryListDTO> ApplicationCategories   = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Application Category";
        private long? dtoLoanTypeId { get; set; } = null;
        private int i_recs = 0;
        private bool isSaving = false;
        private bool isLoading = false;
        private string? ButtonMessage;

        List<ZabDataTable<ApplicationCategoryListDTO>.ColumnDefinition<ApplicationCategoryListDTO>> columns = new()
        {
            new() { Title = "Category Name", Value = x => x.LoanType},                     
            new() { Title = "Remarks", Value = x => x.Remarks },
            new() { Title = "Active", Value = x => x.Active ? "Yes" : "No" },
        };

        protected override async Task OnInitializedAsync()
        {
            await RefreshDataAsync();
        }

        //Load data into he list here, this method can also be called to refresh the list after an Add or Edit operation
        private async Task RefreshDataAsync()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var result = await applicationCategoryService!.GetListAsync();
                ApplicationCategories = result.ToList();
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


        async Task CreateApplicationCategory()
        {
            try
            {
                ButtonMessage = "Create Loan Type";
                offcanvasTitle = "Add Loan Type";
                dtoLoanTypeId = null;
                _model = new CreateApplicationCategoryDTO
                { Active = true};
                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);      //repplace with a logger service 
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Create Loan Type ..", false);
            }
        }

        void EditBankDetails(long id) => Nav.NavigateTo($"/LoanTypePages/edit/{id}");


        //EDIT BANK IN OFF-CANVAS
        async Task EditApplicationCategory(ApplicationCategoryListDTO dto)
        {
            try
            {
                ButtonMessage = "Update Loan Type";
                offcanvasTitle = "Edit Loan Type";
                dtoLoanTypeId = dto.LoanTypeId;
                _model = new CreateApplicationCategoryDTO
                {
                    //BankId = bank.BankId, CreateBank does not have BankID, hence it will pass a null to this field
                    //This will be important in deciding if to call Create or Update in the HandleSubmit method
                    LoanType = dto.LoanType,
                    Remarks = dto.Remarks,
                    Active = dto.Active
                };

                await offcanvas!.Show();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);      //replace this with proper logging
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Edit Loan Type ..", false);
            }
            finally
            {
                // Cleanup or final actions
            }
        }

        protected Task DeleteApplicationCategory()
        {
            return Task.CompletedTask;
        }
        private async Task HandleSubmit()
        {
            if (applicationCategoryService == null) return;
            isSaving = true;

            try
            {
                bool isSuccess = false;
                if (dtoLoanTypeId.HasValue)
                {
                    // UPDATE LOGIC
                    var updateModel = new UpdateApplicationCategoryDTO
                    {
                        LoanTypeId = dtoLoanTypeId.Value,
                        LoanType = _model.LoanType,
                        Remarks = _model.Remarks,
                        Active = _model.Active
                    };
                    i_recs = await applicationCategoryService.UpdateAsync(updateModel);
                    isSuccess = true;
                }
                else
                {
                    // CREATE LOGIC
                    dtoLoanTypeId = await applicationCategoryService.CreateAsync(_model);
                    isSuccess = true;
                }

                if (isSuccess)
                {
                    if (dtoLoanTypeId.HasValue)
                    {
                        _Toast.ShowInfo("Loan Type updated successfully!", "Edit Loan Type");
                    }
                    else
                    {
                        _Toast.ShowInfo("Loan Type created successfully!", "Add Loan Type");
                    }

                    await RefreshDataAsync();    // 1. Refresh the background grid so the user sees the new data

                    if (dtoLoanTypeId.HasValue)
                    {
                        await offcanvas!.Close(); // If it was an EDIT, we usually close the panel as the task is "done"
                    }
                    else
                    {
                        _model = new CreateApplicationCategoryDTO { Active = true };      // If it was an ADD, keep panel open and RESET for the next record                  
                        StateHasChanged();                                // StateHasChanged ensures the form fields clear out
                    }
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"{ex.Message}", "ERROR ENCOUNTERED @Saving bank ..", false);
            }
            finally
            {
                isSaving = false;
            }
        }


    }
}
