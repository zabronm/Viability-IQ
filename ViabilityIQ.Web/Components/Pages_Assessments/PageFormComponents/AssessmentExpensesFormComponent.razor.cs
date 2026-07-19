using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentExpensesFormComponent : ComponentBase
    {
        [Inject] private ISessionService sessionService { get; set; } = default!;
        [Inject] private ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] private IGenericDataRepository<AssessmentExpenses> DataRepository { get; set; } = default!;

        [Parameter] public AssessmentExpenses? ExpenseContext { get; set; }

        private AssessmentExpenses FormModel { get; set; } = new();
        private decimal[] MonthlyValues { get; set; } = new decimal[12];
        private decimal BulkAnnualValueTarget { get; set; }

        private bool IsSubmitting { get; set; } = false;

        protected override void OnParametersSet()
        {
            long assessmentId = sessionService.AssessmentId ?? 0;

            if (ExpenseContext != null)
            {
                // Clone existing record
                FormModel = new AssessmentExpenses
                {
                    AssessmentId = assessmentId,
                    AssessmentExpenseId = ExpenseContext.AssessmentExpenseId,
                    Description = ExpenseContext.Description,
                    ExpenseTypeId = ExpenseContext.ExpenseTypeId,
                    blSendToCashBook = ExpenseContext.blSendToCashBook,
                    Month_1 = ExpenseContext.Month_1,
                    Month_2 = ExpenseContext.Month_2,
                    Month_3 = ExpenseContext.Month_3,
                    Month_4 = ExpenseContext.Month_4,
                    Month_5 = ExpenseContext.Month_5,
                    Month_6 = ExpenseContext.Month_6,
                    Month_7 = ExpenseContext.Month_7,
                    Month_8 = ExpenseContext.Month_8,
                    Month_9 = ExpenseContext.Month_9,
                    Month_10 = ExpenseContext.Month_10,
                    Month_11 = ExpenseContext.Month_11,
                    Month_12 = ExpenseContext.Month_12
                };
                MonthlyValues = FormModel.MonthlyValues;
            }
            else
            {
                FormModel = new() { AssessmentId = assessmentId };
            }
        }

        private void ApplyMonthlyAllocation()
        {
            // Sets every month to the specified amount
            for (int i = 0; i < 12; i++)
            {
                MonthlyValues[i] = FormModel.SameMonthlyAmount;
            }
        }

        private void DistributeAnnualExpensesEvenly()
        {
            decimal slice = Math.Round(BulkAnnualValueTarget / 12m, 0);
            for (int i = 0; i < 12; i++) MonthlyValues[i] = slice;
        }

        private void ToggleSalesPercentage()
        {
            // If we are checking the box, ensure the rate is reset to 0
            if (!FormModel.blPercentageOfSalesUsed)
            {
                FormModel.PercentageOfSalesRate = 0;
            }
        }


        private async Task ExecuteSaveWorkflowAsync()
        {
            //if (FormModel == null || IsSubmitting || string.IsNullOrWhiteSpace(FormModel.Description)) return;
            if (FormModel == null || IsSubmitting) return;

            try
            {
                IsSubmitting = true;
                FormModel.MonthlyValues = MonthlyValues; // Uses your class setter

                bool isExecutionSuccess = await DataRepository.SaveAsync(FormModel);
                var result = SaveResult.SavedAndNew("Expense details archived successfully.");

               await zabCanvasService!.PublishResultAsync(result);

                if (result.ClearForm) 
                                ClearForm();

            }
            catch (Exception ex)
            {
                await zabCanvasService!.PublishResultAsync(new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private void ClearForm()
        {
            FormModel = new AssessmentExpenses
            {
                AssessmentId = sessionService.AssessmentId ?? 0
            };

            MonthlyValues = new decimal[12];
            BulkAnnualValueTarget = 0;
            StateHasChanged();
        }


        private async Task CancelFormAsync() => await zabCanvasService!.HideAsync(SaveResult.Cancel());
    }
}