using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentExpensesPage : ComponentBase
    {
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Parameter] public long AssessmentId { get; set; }

        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Sales Notice:";
        private string AlertMessage { get; set; } = "Verify that your sales values align accurately with your cost of sales allocations for this assessment phase.";

        private string SearchQuery { get; set; } = string.Empty;
        private string ActivePanelTitle { get; set; } = string.Empty;
        private WizardMode ActiveWizardMode { get; set; } = WizardMode.SingleEntryOrEdit;

        private ExpenseClassification? SelectedClassificationFilter { get; set; }
        private bool AreSummariesReady { get; set; } = false;
        private AssessmentFinancialsDto ConsolidatedAssessmentData { get; set; } = new();

        private ExpenseEntryViewModel EditingExpenseContext { get; set; } = new();
        private List<ExpenseEntryViewModel> ExpenseDatasetLines { get; set; } = new();
        private List<SetupSuggestionViewModel> BulkWizardCheckboxList { get; set; } = new();

        private decimal BaselineAnnualSalesReference { get; set; } = 485000m;

        private IEnumerable<ExpenseEntryViewModel> FilteredExpenseLines
        {
            get
            {
                var query = ExpenseDatasetLines.AsEnumerable();

                // Apply Text Search Filter Bound Query Parameters
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    query = query.Where(x => x.ExpenseName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                }

                // Apply Strict Enum Classification Match Criteria
                if (SelectedClassificationFilter.HasValue)
                {
                    query = query.Where(x => x.Classification == SelectedClassificationFilter.Value);
                }

                return query;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            SeedExpensesPipelineSnapshot();
            InitializeWizardDefaults();
            MapDynamicFinancialSummaryPayload();

            // Delays summary panel container injection to initiate cascading load animation transitions
            await Task.Delay(350);
            AreSummariesReady = true;
        }

        private void MapDynamicFinancialSummaryPayload()
        {
            // 1. Assign local actual monthly expenses aggregated directly from the dataset records
            ConsolidatedAssessmentData.MonthlyExpenses = new decimal[12];
            for (int i = 0; i < 12; i++)
            {
                ConsolidatedAssessmentData.MonthlyExpenses[i] = ExpenseDatasetLines.Sum(e => e.MonthlyValues[i]);
            }

            // 2. Fetch external workspace dependencies (Derived from Sales/Stock baseline calculations modules)
            ConsolidatedAssessmentData.MonthlySales = new decimal[12] { 32000, 34000, 31000, 35000, 38000, 42000, 40000, 41000, 45000, 43000, 48000, 52000 };
            ConsolidatedAssessmentData.MonthlyCostOfSales = new decimal[12] { 12000, 13000, 11500, 14000, 15000, 17500, 16000, 16500, 18000, 17000, 20000, 22000 };
            ConsolidatedAssessmentData.MonthlySundryIncome = new decimal[12] { 1000, 1200, 800, 1500, 1100, 1300, 900, 1400, 1500, 1200, 1600, 2000 };

            // 3. Assign structural static balance thresholds
            ConsolidatedAssessmentData.TotalFixedCosts = 45000m;
            ConsolidatedAssessmentData.TotalFixedAssets = 220000m;
            ConsolidatedAssessmentData.AverageStockValue = 18500m;
        }

        private void SeedExpensesPipelineSnapshot()
        {
            ExpenseDatasetLines = new List<ExpenseEntryViewModel>
            {
                new ExpenseEntryViewModel { Id = 1, ExpenseName = "Commercial Office Rental Charges", Classification = ExpenseClassification.Operating, AffectsCashbookDirectly = true, MonthlyValues = new decimal[12] { 8500, 8500, 8500, 8500, 8500, 8500, 9000, 9000, 9000, 9000, 9000, 9000 } },
                new ExpenseEntryViewModel { Id = 2, ExpenseName = "Electricity, Water and Municipal Utilities", Classification = ExpenseClassification.Operating, AffectsCashbookDirectly = true, MonthlyValues = new decimal[12] { 2400, 2600, 2500, 2900, 3400, 3800, 3900, 3700, 3100, 2800, 2500, 2300 } },
                new ExpenseEntryViewModel { Id = 3, ExpenseName = "Logistics Outbound Distribution Freight", Classification = ExpenseClassification.Operating, AffectsCashbookDirectly = true, MonthlyValues = new decimal[12] { 4100, 4500, 5200, 4800, 5100, 5600, 5400, 5900, 6100, 6400, 6800, 7500 } }
            };
        }

        private void InitializeWizardDefaults()
        {
            BulkWizardCheckboxList = new List<SetupSuggestionViewModel>
            {
                new SetupSuggestionViewModel { Id = 101, ExpenseName = "Audit & Statutory Accounting Compliance Subscriptions", Classification = ExpenseClassification.Operating },
                new SetupSuggestionViewModel { Id = 102, ExpenseName = "Corporate Staff Medical Insurance Risk Allocations", Classification = ExpenseClassification.Operating },
                new SetupSuggestionViewModel { Id = 103, ExpenseName = "Marketing Campaigns & Localized Brand Placement Activities", Classification = ExpenseClassification.Operating }
            };
        }

        private async Task OpenNewExpensePanel()
        {
            EditingExpenseContext = new ExpenseEntryViewModel { Id = 0, MonthlyValues = new decimal[12] };
            ActivePanelTitle = "Add Assessment Expenses";
            await InvokeCanvasPanelOpenAsync();
        }

        private async Task OpenEditExpensePanel(ExpenseEntryViewModel expense)
        {
            EditingExpenseContext = expense;
            ActivePanelTitle = "Edit Assessment Expenses";
            await InvokeCanvasPanelOpenAsync();
        }

        private async Task OpenBulkSetupPanel()
        {
            ActivePanelTitle = "Bulk Setup Matrix Wizard";
            await InvokeCanvasPanelOpenAsync();
        }

        private async Task InvokeCanvasPanelOpenAsync()
        {
            if (zabCanvasService != null)
            {
                await zabCanvasService.ShowAsync(new CanvasRequest
                {
                    Title = ActivePanelTitle,
                    Width = 400,
                    ComponentType = typeof(AssessmentExpensesFormComponent),
                    Parameters = new
                    {
                        ExpenseContext = EditingExpenseContext,
                        WizardModeContext = ActiveWizardMode,
                        SuggestionsDataset = BulkWizardCheckboxList,
                        TotalSalesBaseline = BaselineAnnualSalesReference
                    },
                    ResultCallback = HandleExpensesResultAsync
                });
            }
        }

        async Task HandleExpensesResultAsync(SaveResult result)
        {
            if (result.Success)
            {
                SeedExpensesPipelineSnapshot();
                InitializeWizardDefaults();
                MapDynamicFinancialSummaryPayload();
                StateHasChanged();

                _Toast?.ShowSuccess(result.Message, sessionService?.AppTitle ?? string.Empty);
            }
            else if (!result.Cancelled)
            {
                _Toast?.ShowError(result.Message, sessionService?.AppTitle ?? string.Empty);
            }
            else
            {
                _Toast?.ShowInfo("You aborted the operation", sessionService?.AppTitle ?? string.Empty);
            }
            await Task.CompletedTask;
        }

        private async Task HandleSaveExpenseChangesAsync(FormSavePayload payload)
        {
            if (payload.Mode == WizardMode.SingleEntryOrEdit && payload.SingleExpense != null)
            {
                if (payload.SingleExpense.Id == 0)
                {
                    payload.SingleExpense.Id = ExpenseDatasetLines.Count + 1;
                    ExpenseDatasetLines.Add(payload.SingleExpense);
                }
                else
                {
                    var match = ExpenseDatasetLines.FirstOrDefault(x => x.Id == payload.SingleExpense.Id);
                    if (match != null)
                    {
                        match.ExpenseName = payload.SingleExpense.ExpenseName;
                        match.Classification = payload.SingleExpense.Classification;
                        match.AffectsCashbookDirectly = payload.SingleExpense.AffectsCashbookDirectly;
                        match.MonthlyValues = payload.SingleExpense.MonthlyValues;
                    }
                }
            }
            else if (payload.Mode == WizardMode.BulkSetupStep1 && payload.SelectedBulkItems != null)
            {
                foreach (var item in payload.SelectedBulkItems)
                {
                    if (!ExpenseDatasetLines.Any(e => e.ExpenseName == item.ExpenseName))
                    {
                        ExpenseDatasetLines.Add(new ExpenseEntryViewModel
                        {
                            Id = ExpenseDatasetLines.Count + 1,
                            ExpenseName = item.ExpenseName,
                            Classification = item.Classification,
                            AffectsCashbookDirectly = true,
                            MonthlyValues = new decimal[12]
                        });
                    }
                }
            }

            MapDynamicFinancialSummaryPayload();
            StateHasChanged();
            await Task.CompletedTask;
        }

        public enum ExpenseClassification { Capex, Operating, LoanInterest, LoanCapital }
        public enum WizardMode { BulkSetupStep1, SingleEntryOrEdit }

        public class ExpenseEntryViewModel
        {
            public long Id { get; set; }
            public string ExpenseName { get; set; } = string.Empty;
            public ExpenseClassification Classification { get; set; }
            public bool AffectsCashbookDirectly { get; set; }
            public decimal[] MonthlyValues { get; set; } = new decimal[12];
        }

        public class SetupSuggestionViewModel
        {
            public long Id { get; set; }
            public string ExpenseName { get; set; } = string.Empty;
            public ExpenseClassification Classification { get; set; }
            public bool IsSelected { get; set; }
        }

        public class FormSavePayload
        {
            public WizardMode Mode { get; set; }
            public ExpenseEntryViewModel SingleExpense { get; set; }
            public List<SetupSuggestionViewModel> SelectedBulkItems { get; set; }
        }
    }
}