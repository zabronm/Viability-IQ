using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
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
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }

        [Parameter] public long AssessmentId { get; set; }


        private AssessmentFinancialsDto ConsolidatedAssessmentData { get; set; } = new();
        private List<UnifiedExpenseViewModel> ExpenseStreams { get; set; } = new();
        private bool IsLoading { get; set; } = true;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "EXPENSES:";
        private string AlertMessage { get; set; } = "Supply expenses details in this section.";

        private ExpenseTypeEnum? SelectedFilterType { get; set; }
        private string SearchQuery { get; set; } = string.Empty;
        private long SelectedFilterId { get; set; } = 0;
        private decimal GrandTotalRevenue => FilteredExpenseStreams?.Sum(c => c.MonthlyValues.Sum()) ?? 0;

        private IEnumerable<UnifiedExpenseViewModel> FilteredExpenseStreams =>
                            ExpenseStreams.Where(x =>
                            (string.IsNullOrWhiteSpace(SearchQuery) || x.ExpenseItemName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
                            (SelectedFilterId == 0 || (long)x.TypeId == SelectedFilterId));

        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsLoading = true;
                AssessmentId = sessionService!.AssessmentId ?? 0;
                await LoadAndMapExpensesData();
                await CreateSummaries();
            }
            catch (Exception ex)
            {
                _Toast?.ShowError(ex.Message);
                ExpenseStreams = new();
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        async Task LoadAndMapExpensesData()
        {
            try
            {
                var result = await ViqCrudService!.GetListAsync<AssessmentExpensesDto>("vw_assessment_expenses_list",
                new { AssessmentId }, "AssessmentId");

                if (result != null)
                {
                    ExpenseStreams = result.Select(s => new UnifiedExpenseViewModel
                    {
                        Id = s.AssessmentExpensesId,
                        Description = s.Description ?? "N/A",
                        ExpenseItemId = s.ExpenseItemId,
                        ExpenseItemName = s.ExpenseItemName ?? "N/A", // Direct map
                        TypeId = (long)s.ExpenseTypeId,          // Direct cast
                        TypeName = s.ExpenseTypeName ?? "N/A",   // Direct map
                        blSendToCashBook = s.blSendToCashBook,
                        blPercentageOfSalesUsed = s.blPercentageOfSalesUsed,
                        MonthlyValues = new decimal[] { s.Month_1, s.Month_2, s.Month_3, s.Month_4, s.Month_5, s.Month_6,
                                                    s.Month_7, s.Month_8, s.Month_9, s.Month_10, s.Month_11, s.Month_12 }
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _Toast!.ShowError(ex.Message, sessionService!.AppTitle);
            }
        }

        private async Task CreateSummaries()
        {
            // Define the list of IDs that you consider to be "Expenses"
            // Replace 1 and 2 with the actual IDs from your database lookups
            var expenseTypeIds = new List<long> { 1, 2 };

            ConsolidatedAssessmentData.MonthlyExpenses = ExpenseStreams
                .Where(x => expenseTypeIds.Contains(x.TypeId))
                .Aggregate(new decimal[12], (acc, cur) =>
                {
                    for (int i = 0; i < 12; i++) acc[i] += cur.MonthlyValues[i];
                    return acc;
                });

            await Task.CompletedTask;

        }

        private async Task AddExpenseStream() => await OpenExpenseFormPanel(new UnifiedExpenseViewModel());


        private async Task OpenExpenseFormPanel(UnifiedExpenseViewModel stream)
        {
            var expense = new AssessmentExpenses
            {
                AssessmentExpenseId = stream.Id,
                ExpenseItemId = stream.ExpenseItemId,
                Description = stream.Description,
                ExpenseTypeId = stream.TypeId,
                MonthlyValues = stream.MonthlyValues
            };


            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = stream.Id == 0 ? "Add Expense Stream" : "Edit Expense Stream",
                Width = 400,
                ComponentType = typeof(AssessmentExpensesFormComponent),

                Parameters = new
                {
                    //ExpenseContext = stream
                    ExpenseContext = expense
                },
                ResultCallback = OnSaveComplete
            });
        }


        //=================  IMPLEMENT CALL BACK SO YOU READ THE SAVERESULT OBJECT ====================
        async Task OnSaveComplete(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(result.Message, sessionService!.AppTitle);
                if (result.RefreshGrid)
                    await LoadAndMapExpensesData();

                StateHasChanged();
            }
            else
            {
                _Toast!.ShowError(result.Message, sessionService!.AppTitle);
            }

        }

        private async Task OpenBulkImport()
        {
            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = "Bulk Expenses Import",
                Width = 700,
                ComponentType = typeof(BulkExpensesImportComponent),
                Parameters = new { AssessmentId },
                ResultCallback = OnSaveComplete
            });
        }
    }
}