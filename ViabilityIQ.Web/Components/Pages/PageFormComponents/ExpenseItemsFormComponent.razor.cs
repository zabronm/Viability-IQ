using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class ExpenseItemsFormComponent
    {
        [Inject] private IGenericDataRepository<ExpenseItems> expenseRepository { get; set; } = default!;

        [Parameter] public long ExpenseItemId { get; set; } = 0;
        [Parameter] public EventCallback<SaveResult> OnSavedSuccess { get; set; }

        private ExpenseItems expenseModel = new();
        private bool isProcessingData = false;
        private bool isRowActive = true;

        protected override async Task OnParametersSetAsync()
        {
            if (ExpenseItemId != 0)
            {
                isProcessingData = true;
                try
                {
                    var record = await expenseRepository.GetByIdAsync(ExpenseItemId);
                    if (record != null)
                    {
                        expenseModel = record;
                        isRowActive = expenseModel.Active;
                    }
                }
                finally
                {
                    isProcessingData = false;
                }
            }
            else
            {
                expenseModel = new ExpenseItems();
                isRowActive = true;
            }
        }

        protected async Task HandleFormSubmissionAsync()
        {
            isProcessingData = true;
            var finalResult = new SaveResult();

            try
            {
                // Assign layout switch criteria state properties straight into model context metadata
                expenseModel.Active = isRowActive;

                // Invoking unified Dapper Save layer method wrapper matching your layout patterns
                bool operationSuccess = await expenseRepository.SaveAsync(expenseModel);

                if (operationSuccess)
                {
                    finalResult.Success = true;                    
                    finalResult.ClosePanel = true;
                    finalResult.Message = ExpenseItemId == 0
                        ? "New expenditure tracker code logged successfully."
                        : "Expenditure definition layout adjusted successfully.";
                }
                else
                {
                    finalResult.Success = false;                   
                    finalResult.ClosePanel = false;
                    finalResult.Message = "The persistence context engine returned false. Commit rejected.";
                }

                await OnSavedSuccess.InvokeAsync(finalResult);
            }
            catch (Exception ex)
            {
                finalResult.Success = false;                
                finalResult.ClosePanel = false;
                finalResult.Message = $"Data Persistence Layer Exception intercepted: {ex.Message}";

                await OnSavedSuccess.InvokeAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }
    }
}