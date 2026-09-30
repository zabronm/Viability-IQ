using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;
using ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;


namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentExpensesPage : ComponentBase, IAsyncDisposable
    {
        #region Injected Services

        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] ILogger<AssessmentExpensesPage>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private AssessmentFinancialsDto ConsolidatedAssessmentData { get; set; } = new();
        private List<UnifiedExpenseViewModel> ExpenseStreams { get; set; } = new();
        private bool IsLoading { get; set; } = true;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "EXPENSES:";
        private string AlertMessage { get; set; } = "Supply expense details in this section.";

        private ExpenseTypeEnum? SelectedFilterType { get; set; }
        private string SearchQuery { get; set; } = string.Empty;
        private long SelectedFilterId { get; set; } = 0;
        private decimal GrandTotalExpenses => FilteredExpenseStreams?.Sum(c => c.MonthlyValues.Sum()) ?? 0;

        private IEnumerable<UnifiedExpenseViewModel> FilteredExpenseStreams =>
            ExpenseStreams.Where(x =>
                (string.IsNullOrWhiteSpace(SearchQuery) || x.ExpenseItemName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
                (SelectedFilterId == 0 || (long)x.TypeId == SelectedFilterId));

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AssessmentId = sessionService?.AssessmentId ?? 0;

                Logger?.LogInformation(
                    "AssessmentExpensesPage initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadAndMapExpensesData();
                await CreateSummaries();
                IsLoading = false;

                // Subscribe to projection changes
                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug("AssessmentExpensesPage subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing AssessmentExpensesPage");
                _Toast?.ShowError(ex.Message);
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        async Task LoadAndMapExpensesData()
        {
            try
            {
                Logger?.LogDebug("Loading expenses data for assessment {AssessmentId}", AssessmentId);

                var result = await ViqCrudService!.GetListAsync<AssessmentExpensesDto>("vw_assessment_expenses_list",
                    new { AssessmentId }, "AssessmentId");

                if (result != null)
                {
                    ExpenseStreams = result.Select(s => new UnifiedExpenseViewModel
                    {
                        Id = s.AssessmentExpensesId,
                        Description = s.Description ?? "N/A",
                        ExpenseItemId = s.ExpenseItemId,
                        ExpenseItemName = s.ExpenseItemName ?? "N/A",
                        TypeId = (long)s.ExpenseTypeId,
                        TypeName = s.ExpenseTypeName ?? "N/A",
                        blSendToCashBook = s.blSendToCashBook,
                        blPercentageOfSalesUsed = s.blPercentageOfSalesUsed,
                        MonthlyValues = new decimal[] { s.Month_1, s.Month_2, s.Month_3, s.Month_4, s.Month_5, s.Month_6,
                                                    s.Month_7, s.Month_8, s.Month_9, s.Month_10, s.Month_11, s.Month_12 }
                    }).ToList();

                    Logger?.LogInformation(
                        "Loaded {ExpenseCount} expense records for assessment {AssessmentId}",
                        ExpenseStreams.Count, AssessmentId);

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading expenses data for assessment {AssessmentId}", AssessmentId);
                _Toast?.ShowError(ex.Message, sessionService?.AppTitle);
            }
        }

        private async Task CreateSummaries()
        {
            // Define the list of IDs that you consider to be "Expenses"
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
                Parameters = new { ExpenseContext = expense },
                ResultCallback = OnSaveComplete
            });
        }

        async Task OnSaveComplete(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(result.Message, sessionService!.AppTitle);
                if (result.RefreshGrid)
                {
                    await LoadAndMapExpensesData();

                    // ✅ TRIGGER CASHFLOW RECALCULATION
                    Logger?.LogInformation("Triggering cashflow recalculation after expense save for assessment {AssessmentId}", AssessmentId);
                    await projectionStateManager!.InvalidateDataAsync("expenses", AssessmentId, AssessmentId);
                }
            }
            else
            {
                _Toast!.ShowError(result.Message, sessionService!.AppTitle);
            }
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            // Reload expenses when any projection data changes for this assessment
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading expenses",
                    AssessmentId);

                InvokeAsync(async () => await LoadAndMapExpensesData());
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

        #endregion

        #region Disposal

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            try
            {
                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged -= OnProjectionChanged;
                    Logger?.LogDebug("AssessmentExpensesPage unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing AssessmentExpensesPage");
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}