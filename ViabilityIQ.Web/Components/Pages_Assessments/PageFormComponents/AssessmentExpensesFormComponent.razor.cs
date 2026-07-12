using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ViabilityIQ.Web.Components.Pages_Assessments.AssessmentExpensesPage;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentExpensesFormComponent : ComponentBase
    {
        [Parameter] public ExpenseEntryViewModel ExpenseContext { get; set; }
        [Parameter] public WizardMode WizardModeContext { get; set; }
        [Parameter] public List<SetupSuggestionViewModel> SuggestionsDataset { get; set; }
        [Parameter] public decimal TotalSalesBaseline { get; set; }

        [Parameter] public EventCallback<FormSavePayload> OnSave { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        private ExpenseEntryViewModel FormModel { get; set; } = new();
        private List<SetupSuggestionViewModel> LocalSuggestions { get; set; } = new();

        private decimal BulkAnnualValueTarget { get; set; }
        private decimal SalesPercentageTarget { get; set; }

        protected override void OnParametersSet()
        {
            if (WizardModeContext == WizardMode.SingleEntryOrEdit && ExpenseContext != null)
            {
                // Isolate model editing state via an explicit clone operation
                FormModel = new ExpenseEntryViewModel
                {
                    Id = ExpenseContext.Id,
                    ExpenseName = ExpenseContext.ExpenseName,
                    Classification = ExpenseContext.Classification,
                    AffectsCashbookDirectly = ExpenseContext.AffectsCashbookDirectly,
                    MonthlyValues = (decimal[])ExpenseContext.MonthlyValues.Clone()
                };
            }
            else if (WizardModeContext == WizardMode.BulkSetupStep1 && SuggestionsDataset != null)
            {
                // Deep clone suggestions configuration dataset array
                LocalSuggestions = SuggestionsDataset.Select(s => new SetupSuggestionViewModel
                {
                    Id = s.Id,
                    ExpenseName = s.ExpenseName,
                    Classification = s.Classification,
                    IsSelected = s.IsSelected
                }).ToList();
            }
        }

        private void DistributeAnnualExpensesEvenly()
        {
            if (BulkAnnualValueTarget <= 0) return;
            decimal distributedValue = Math.Round(BulkAnnualValueTarget / 12m, 0);
            for (int i = 0; i < 12; i++)
            {
                FormModel.MonthlyValues[i] = distributedValue;
            }
            BulkAnnualValueTarget = 0;
        }

        private void DistributeExpensesBySalesPercentage()
        {
            if (SalesPercentageTarget <= 0 || TotalSalesBaseline <= 0) return;
            decimal calculatedProportionalAllocation = (TotalSalesBaseline * (SalesPercentageTarget / 100m)) / 12m;
            decimal roundedResult = Math.Round(calculatedProportionalAllocation, 0);

            for (int i = 0; i < 12; i++)
            {
                FormModel.MonthlyValues[i] = roundedResult;
            }
            SalesPercentageTarget = 0;
        }

        private async Task SaveSingleExpenseAsync()
        {
            if (OnSave.HasDelegate)
            {
                await OnSave.InvokeAsync(new FormSavePayload
                {
                    Mode = WizardMode.SingleEntryOrEdit,
                    SingleExpense = FormModel
                });
            }
        }

        private async Task SaveBulkSelectionsAsync()
        {
            if (OnSave.HasDelegate)
            {
                var selectedItems = LocalSuggestions.Where(x => x.IsSelected).ToList();
                await OnSave.InvokeAsync(new FormSavePayload
                {
                    Mode = WizardMode.BulkSetupStep1,
                    SelectedBulkItems = selectedItems
                });
            }
        }

        private async Task CancelFormAsync()
        {
            if (OnCancel.HasDelegate)
            {
                await OnCancel.InvokeAsync();
            }
        }
    }
}