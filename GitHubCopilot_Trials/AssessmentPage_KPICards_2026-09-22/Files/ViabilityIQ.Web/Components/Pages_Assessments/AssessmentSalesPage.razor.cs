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
using ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentSalesPage : IAsyncDisposable
    {
        #region Injected Services

        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] ILogger<AssessmentSalesPage>? Logger { get; set; }
        [Inject] IGenericDataRepository<AssessmentProjectionAssumptions> AssumptionsRepository { get; set; } = default!;

        #endregion

        #region Parameters
        [Parameter] public long AssessmentId { get; set; }
        #endregion

        #region Private Fields
        private ZabConfirmDialogComponent? ConfirmDeleteDialog { get; set; } = default!;
        private AssessmentFinancialsDto ConsolidatedAssessmentData { get; set; } = new();
        private List<UnifiedIncomeViewModel> IncomeStreams { get; set; } = new();
        private AssessmentProjectionAssumptions ProjectionAssumptions { get; set; } = new();
        private bool IsLoading { get; set; } = true;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "SALES:";
        private string AlertMessage { get; set; } = "Supply income/revenue details in this section.";

        private IncomeTypeEnum? SelectedFilterType { get; set; }
        private string SearchQuery { get; set; } = string.Empty;
        private long SelectedFilterId { get; set; } = 0;
        private decimal GrandTotalRevenue => FilteredIncomeStreams?.Sum(c => c.MonthlyValues.Sum()) ?? 0;
        private decimal[] SalesMonthlyTotals => IncomeStreams
            .Where(x => x.TypeId is 1 or 2)
            .Aggregate(new decimal[12], (totals, stream) =>
            {
                for (var month = 0; month < Math.Min(12, stream.MonthlyValues.Length); month++)
                    totals[month] += stream.MonthlyValues[month];
                return totals;
            });
        private decimal TotalSales => SalesMonthlyTotals.Sum();
        private decimal AverageMonthlySales => TotalSales / 12m;
        private decimal SalesGrowth => SalesMonthlyTotals[0] == 0m
            ? 0m
            : (SalesMonthlyTotals[11] - SalesMonthlyTotals[0]) / SalesMonthlyTotals[0] * 100m;
        private decimal CostOfSalesRate => Math.Clamp(ProjectionAssumptions.CostOfSalesPercentage ?? 0m, 0m, 100m);
        private decimal GrossProfit => TotalSales * (1m - CostOfSalesRate / 100m);
        private decimal GrossMargin => TotalSales == 0m ? 0m : GrossProfit / TotalSales * 100m;
        private decimal CreditSales => TotalSales * (1m - Math.Clamp(ProjectionAssumptions.CashSalesPercentage, 0m, 100m) / 100m);
        private IReadOnlyList<AssessmentKpiCardItem> SalesKpis =>
        [
            new("Total Sales", Money(TotalSales), "Projected 12-month revenue", "bi bi-graph-up-arrow", "kpi-blue"),
            new("Sales Growth", Percent(SalesGrowth), "Month 1 to Month 12", "bi bi-arrow-up-right", "kpi-teal"),
            new("Average Monthly Sales", Money(AverageMonthlySales), "Average projected revenue", "bi bi-calendar3", "kpi-purple"),
            new("Gross Profit", Money(GrossProfit), $"After {CostOfSalesRate:N1}% cost of sales", "bi bi-bar-chart-line", "kpi-slate"),
            new("Gross Margin", Percent(GrossMargin), "Gross profit as % of sales", "bi bi-pie-chart", "kpi-cyan"),
            new("Credit Sales", Money(CreditSales), $"{100m - ProjectionAssumptions.CashSalesPercentage:N1}% of sales", "bi bi-credit-card", "kpi-red")
        ];

        // Sorting State
        private string currentSortColumn = "Description";
        private bool isAscending = true;

        private IEnumerable<UnifiedIncomeViewModel> FilteredIncomeStreams
        {
            get
            {
                var query = IncomeStreams.Where(x =>
                    (string.IsNullOrWhiteSpace(SearchQuery) ||
                     x.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
                    (SelectedFilterId == 0 || (long)x.TypeId == SelectedFilterId));

                query = currentSortColumn switch
                {
                    "IncomeType" => isAscending ? query.OrderBy(x => x.TypeName) : query.OrderByDescending(x => x.TypeName),
                    _ => isAscending ? query.OrderBy(x => x.Description) : query.OrderByDescending(x => x.Description)
                };

                return query;
            }
        }

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AssessmentId = sessionService.AssessmentId ?? 0;

                Logger.LogInformation(
                    "AssessmentSalesPage initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadAndMapSalesData();
                await LoadProjectionAssumptionsAsync();
                await CreateSummaries();
                IsLoading = false;

                // Subscribe to projection changes
                projectionStateManager.ProjectionChanged += OnProjectionChanged;

                Logger.LogDebug("AssessmentSalesPage subscribed to ProjectionChanged events");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing AssessmentSalesPage");
                IsLoading = false;
            }
        }

        #endregion

        #region Sorting Helper Methods

        private void SortTable(string columnName)
        {
            if (currentSortColumn == columnName)
            {
                isAscending = !isAscending;
            }
            else
            {
                currentSortColumn = columnName;
                isAscending = true;
            }
        }

        private string GetSortIcon(string columnName)
        {
            if (currentSortColumn != columnName) return "bi bi-arrow-down-up text-muted opacity-50";
            return isAscending ? "bi bi-arrow-up text-primary" : "bi bi-arrow-down text-primary";
        }

        #endregion

        #region Private Methods

        async Task LoadAndMapSalesData()
        {
            try
            {
                Logger.LogDebug("Loading sales data for assessment {AssessmentId}", AssessmentId);

                var result = await ViqCrudService.GetListAsync<AssessmentSalesDto>(
                    "vw_assessment_sales_list",
                    new { AssessmentId },
                    "AssessmentSalesId");

                if (result != null)
                {
                    IncomeStreams = result.Select(s => new UnifiedIncomeViewModel
                    {
                        Id = s.AssessmentSalesId,
                        Description = s.Description ?? "N/A",
                        TypeId = (long)s.IncomeTypeId,
                        TypeName = s.IncomeTypeName ?? "N/A",
                        MonthlyValues = new decimal[]
                        {
                            s.Month_1, s.Month_2, s.Month_3, s.Month_4, s.Month_5, s.Month_6,
                            s.Month_7, s.Month_8, s.Month_9, s.Month_10, s.Month_11, s.Month_12
                        }
                    }).ToList();

                    Logger.LogInformation(
                        "Loaded {SalesCount} sales records for assessment {AssessmentId}",
                        IncomeStreams.Count, AssessmentId);

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading sales data for assessment {AssessmentId}", AssessmentId);
            }
        }

        private async Task LoadProjectionAssumptionsAsync()
        {
            ProjectionAssumptions = (await AssumptionsRepository.GetAllAsync(item =>
                    item.AssessmentId == AssessmentId && item.Active))
                .OrderByDescending(item => item.AssessmentProjectionAssumptionsId)
                .FirstOrDefault()
                ?? new AssessmentProjectionAssumptions { AssessmentId = AssessmentId };
        }

        private static string Money(decimal value) => $"R {value:N0}";
        private static string Percent(decimal value) => $"{value:N1}%";

        private async Task CreateSummaries()
        {
            var salesTypeIds = new List<long> { 1, 2 };

            ConsolidatedAssessmentData.MonthlySales = IncomeStreams
                .Where(x => salesTypeIds.Contains(x.TypeId))
                .Aggregate(new decimal[12], (acc, cur) =>
                {
                    for (int i = 0; i < 12; i++) acc[i] += cur.MonthlyValues[i];
                    return acc;
                });

            await Task.CompletedTask;
        }

        private async Task AddIncomeStream() =>
            await OpenIncomeFormPanel(new UnifiedIncomeViewModel());

        private async Task OpenIncomeFormPanel(UnifiedIncomeViewModel stream)
        {
            await zabCanvasService.ShowAsync(new CanvasRequest
            {
                Title = stream.Id == 0 ? "Add Revenue Stream" : "Edit Revenue Stream",
                Width = 400,
                ComponentType = typeof(IncomeSalesFormComponent),
                Parameters = new { IncomeContext = stream },
                ResultCallback = OnSaveComplete
            });
        }

        async Task OnSaveComplete(SaveResult result)
        {
            if (result.Success)
            {
                _Toast.ShowSuccess(result.Message, sessionService.AppTitle);
                if (result.RefreshGrid)
                    await LoadAndMapSalesData();
            }
            else
            {
                _Toast.ShowError(result.Message, sessionService.AppTitle);
            }
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            if (e.AssessmentId == AssessmentId && e.DataType == "sales")
            {
                Logger.LogInformation(
                    "Sales projection changed, refreshing sales data for assessment {AssessmentId}",
                    AssessmentId);

                InvokeAsync(async () => await LoadAndMapSalesData());
            }
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
                    Logger.LogDebug("AssessmentSalesPage unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing AssessmentSalesPage");
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}