using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
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
        #region Injected Dependencies

        [Inject] private ISessionService sessionService { get; set; } = default!;
        [Inject] private ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] private IGenericDataRepository<AssessmentExpenses> DataRepository { get; set; } = default!;
        [Inject] private IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] private ILogger<AssessmentExpensesFormComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public AssessmentExpenses? ExpenseContext { get; set; }

        #endregion

        #region Private Fields

        private AssessmentExpenses FormModel { get; set; } = new();
        private decimal[] MonthlyValues { get; set; } = new decimal[12];
        private decimal BulkAnnualValueTarget { get; set; }
        private bool IsSubmitting { get; set; } = false;

        #endregion

        #region Lifecycle Methods

        protected override void OnParametersSet()
        {
            long assessmentId = sessionService.AssessmentId ?? 0;

            if (ExpenseContext != null && ExpenseContext.AssessmentExpenseId > 0)
            {
                // Clone existing record
                FormModel = new AssessmentExpenses
                {
                    AssessmentId = assessmentId,
                    AssessmentExpenseId = ExpenseContext.AssessmentExpenseId,
                    Description = ExpenseContext.Description,
                    ExpenseTypeId = ExpenseContext.ExpenseTypeId,
                    ExpenseItemId = ExpenseContext.ExpenseItemId,
                    blSendToCashBook = ExpenseContext.blSendToCashBook,
                    blPercentageOfSalesUsed = ExpenseContext.blPercentageOfSalesUsed,
                    PercentageOfSalesRate = ExpenseContext.PercentageOfSalesRate,
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
                MonthlyValues = new decimal[12];
            }
        }

        #endregion

        #region Private Methods

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
            if (!FormModel.blPercentageOfSalesUsed)
            {
                FormModel.PercentageOfSalesRate = 0;
            }
        }

        private async Task ExecuteSaveWorkflowAsync()
        {
            if (FormModel == null || IsSubmitting) return;

            try
            {
                IsSubmitting = true;
                FormModel.MonthlyValues = MonthlyValues;

                Logger?.LogInformation(
                    "Saving expense for assessment {AssessmentId}",
                    FormModel.AssessmentId);

                bool isExecutionSuccess = await DataRepository.SaveAsync(FormModel);

                if (isExecutionSuccess)
                {
                    var result = SaveResult.SavedAndNew("Expense details saved successfully.");

                    // ✅ TRIGGER CASHFLOW RECALCULATION
                    Logger?.LogInformation(
                        "Invalidating cashflow after expense save for assessment {AssessmentId}",
                        FormModel.AssessmentId);

                    await projectionStateManager!.InvalidateDataAsync("expenses", FormModel.AssessmentId, FormModel.AssessmentId);

                    await zabCanvasService!.PublishResultAsync(result);

                    if (result.ClearForm)
                        ClearForm();
                }
                else
                {
                    await zabCanvasService!.PublishResultAsync(
                        new SaveResult { Success = false, Message = "Failed to save expense" });
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error saving expense for assessment {AssessmentId}", FormModel.AssessmentId);
                await zabCanvasService!.PublishResultAsync(
                    new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
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

        #endregion
    }
}