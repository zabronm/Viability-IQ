using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Shared.UtilityServices;
using ViabilityIQ.Web.Services;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments;
using ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockPage : ComponentBase, IAsyncDisposable
    {
        #region Injected Services

        [Inject] private IGenericDataRepository<AssessmentStock> DataRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> AssessmentRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssessmentSalesCategory> CategoryRepository { get; set; } = default!;
        [Inject] private ICashflowProjectionService ProjectionService { get; set; } = default!;
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] ILogger<AssessmentStockPage>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields
        private ZabConfirmDialogComponent? ConfirmDeleteDialog { get; set; } = default!;
        private List<AssessmentStock> StockDataList = new();
        private List<AssessmentSalesCategory> CategoryDataList = new();
        private List<UnifiedStockViewModel> FilteredStockList = new();
        private decimal? ProjectedClosingStock;
        private int AssessmentModeId = (int)AssessmentMode.Trading;
        private bool IsLoading = true;
        private string? LoadErrorMessage;
        private string _searchTerm = string.Empty;
        private string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm == value)
                {
                    return;
                }

                _searchTerm = value;
                FilterData();
            }
        }

        #endregion

        #region Properties

        private decimal GrandTotalStockValue => FilteredStockList?.Sum(x => x.MonthlyValues.Sum()) ?? 0;
        private decimal[] StockMonthlyTotals => Enumerable.Range(1, 12)
            .Select(month => StockDataList.Sum(item => GetStockMonthValue(item, month)))
            .ToArray();
        private decimal TotalStockValue => StockMonthlyTotals.Sum();
        private decimal AverageMonthlyStock => TotalStockValue / 12m;
        private decimal OpeningStock => StockDataList.Sum(item => item.OpeningBalance);
        private decimal PeakStock => StockMonthlyTotals.Max();
        private int PeakStockMonth => Array.IndexOf(StockMonthlyTotals, PeakStock) + 1;
        private decimal VatInclusiveStock => StockDataList
            .Where(item => item.blIncludeVAT)
            .Sum(item => Enumerable.Range(1, 12).Sum(month => GetStockMonthValue(item, month)));
        private bool IsManufacturingMode =>
            AssessmentModeId is (int)AssessmentMode.ManufacturingSimplified
                or (int)AssessmentMode.Manufacturing;
        private bool IsFullManufacturingMode =>
            AssessmentModeId == (int)AssessmentMode.Manufacturing;
        private bool IsLegacyManufacturingMode =>
            AssessmentModeId == (int)AssessmentMode.ManufacturingSimplified;
        private bool IsServiceMode =>
            AssessmentModeId == (int)AssessmentMode.Service;
        private IReadOnlyList<AssessmentKpiCardItem> StockKpis =>
            IsServiceMode
                ? []
                : IsManufacturingMode
                    ?
                    [
                        new("Planned Production", DisplayUtility.FormatNumber(StockDataList.Sum(item => item.MonthlyProductionUnits.Sum()), sessionService?.CultureName, "N2"), "Planned units across the 12-month assessment", "bi bi-boxes", "kpi-blue"),
                        new("Standard Unit Cost", Money(StockDataList.FirstOrDefault()?.StandardUnitCost ?? 0m), "Cost entered per manufactured unit", "bi bi-calculator", "kpi-teal"),
                        new("Opening Stock", Money(OpeningStock), "Opening finished-goods balances", "bi bi-box-arrow-in-right", "kpi-purple"),
                        new("Closing Stock", ProjectedClosingStock.HasValue ? Money(ProjectedClosingStock.Value) : "Unavailable", "Calculated Month 12 inventory", "bi bi-box-arrow-right", "kpi-slate")
                    ]
                    :
                    [
                        new("Annual Stock Purchases", Money(TotalStockValue), "Projected purchases/additions over 12 months", "bi bi-boxes", "kpi-blue"),
                        new("Average Monthly Purchases", Money(AverageMonthlyStock), "Average monthly purchases", "bi bi-calendar3", "kpi-teal"),
                        new("Opening Stock", Money(OpeningStock), "Sum of active stock item opening balances", "bi bi-box-arrow-in-right", "kpi-purple"),
                        new("Closing Stock", ProjectedClosingStock.HasValue ? Money(ProjectedClosingStock.Value) : "Unavailable", "Calculated Month 12 inventory", "bi bi-box-arrow-right", "kpi-slate"),
                        new("Peak Purchase Month", Money(PeakStock), $"Highest purchases in Month {PeakStockMonth}", "bi bi-graph-up", "kpi-cyan"),
                        new("VAT-inclusive Purchases", Money(VatInclusiveStock), "Purchases marked VAT inclusive", "bi bi-percent", "kpi-red")
                    ];

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (AssessmentId <= 0)
                    AssessmentId = sessionService?.AssessmentId ?? 0;

                if (AssessmentId <= 0)
                {
                    LoadErrorMessage = "An assessment is required to load stock and cashflow projections.";
                    Logger?.LogWarning("AssessmentStockPage opened without a valid assessment id.");
                    return;
                }

                var assessment = await AssessmentRepository.GetByIdAsync(AssessmentId);
                if (assessment is null)
                {
                    LoadErrorMessage = "The selected assessment could not be found.";
                    return;
                }

                AssessmentModeId = assessment.AssessmentModeId is >= 1 and <= 4
                    ? assessment.AssessmentModeId
                    : (int)AssessmentMode.Trading;

                Logger?.LogInformation(
                    "AssessmentStockPage initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadStockData();

                // Subscribe to projection changes
                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug("AssessmentStockPage subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing AssessmentStockPage");
                _Toast?.ShowError(ex.Message, sessionService?.AppTitle ?? "Stock");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadStockData()
        {
            if (AssessmentId <= 0)
                return;

            try
            {
                IsLoading = true;

                Logger?.LogDebug("Loading stock data for assessment {AssessmentId}", AssessmentId);

                var stockTask = DataRepository.GetAllAsync(x =>
                    x.AssessmentId == AssessmentId && x.Active);
                var categoriesTask = CategoryRepository.GetAllAsync(x =>
                    x.AssessmentId == AssessmentId);
                await Task.WhenAll(stockTask, categoriesTask);
                StockDataList = (await stockTask).ToList();
                CategoryDataList = (await categoriesTask).ToList();
                try
                {
                    ProjectedClosingStock =
                        (await ProjectionService.CalculateAsync(AssessmentId)).Summary.ClosingStock;
                }
                catch (Exception exception)
                {
                    ProjectedClosingStock = null;
                    Logger?.LogWarning(
                        exception,
                        "Could not calculate closing stock for assessment {AssessmentId}",
                        AssessmentId);
                }

                Logger?.LogInformation(
                    "Loaded {StockCount} stock records for assessment {AssessmentId}",
                    StockDataList.Count, AssessmentId);

                FilterData();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading stock data for assessment {AssessmentId}", AssessmentId);
                _Toast!.ShowError("Error encountered: Could not load selected stock payload, please retry.", sessionService!.AppTitle);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private void FilterData()
        {
            // Projection: Convert DTOs to the Unified ViewModel
            var categories = CategoryDataList.ToDictionary(
                category => category.AssessmentSalesCategoryId,
                category => category.AssessmentSalesCategoryName ?? "Unknown");
            var sourceList = string.IsNullOrWhiteSpace(SearchTerm)
                ? StockDataList
                : StockDataList.Where(x =>
                    (x.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (categories.TryGetValue(x.AssessmentSalesCategoryId, out var categoryName)
                        && categoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)));

            FilteredStockList = sourceList.Select(item => new UnifiedStockViewModel
            {
                Id = item.AssessmentStockId,
                AssessmentSalesCategoryName = categories.TryGetValue(item.AssessmentSalesCategoryId, out var categoryName)
                    ? categoryName
                    : "Unknown",
                Description = item.Description ?? string.Empty,
                OpeningBalance = item.OpeningBalance,
                blIncludeVAT = item.blIncludeVAT,
                MonthlyValues = item.MonthlyValues,
                StandardUnitCost = item.StandardUnitCost,
                MonthlyProductionUnits = item.MonthlyProductionUnits
            }).ToList();
        }

        private decimal GetMonthValue(UnifiedStockViewModel item, int month) =>
            IsManufacturingMode
                ? item.MonthlyProductionUnits[month - 1]
                : item.MonthlyValues[month - 1];

        private decimal GetTotalForMonth(int month) => FilteredStockList.Sum(x =>
            IsManufacturingMode
                ? x.MonthlyProductionUnits[month - 1]
                : x.MonthlyValues[month - 1]);

        private decimal GetGrandTotalStockValue() => FilteredStockList.Sum(x =>
            IsManufacturingMode
                ? x.MonthlyProductionUnits.Sum()
                : x.MonthlyValues.Sum());

        private static decimal GetStockMonthValue(AssessmentStock item, int month) => month switch
        {
            1 => item.Month_1, 2 => item.Month_2, 3 => item.Month_3, 4 => item.Month_4,
            5 => item.Month_5, 6 => item.Month_6, 7 => item.Month_7, 8 => item.Month_8,
            9 => item.Month_9, 10 => item.Month_10, 11 => item.Month_11, 12 => item.Month_12,
            _ => 0m
        };

        private string Money(decimal value) =>
            DisplayUtility.FormatCurrency(value, sessionService?.CurrencyCode, sessionService?.CultureName);

        private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs e)
        {
            // Reload stock when any projection data changes for this assessment
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading stock",
                    AssessmentId);

                InvokeAsync(async () => await LoadStockData());
            }
        }

        private async Task OpenStockDataEntryPanel(long stockId)
        {
            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = stockId == 0 ? "Add Stock Item" : "Edit Stock Item",
                ComponentType = typeof(AssessmentStockFormComponent),
                Width = 400,
                Parameters = new
                {
                    AssessmentId,
                    AssessmentStockId = stockId,
                    AssessmentModeId
                },
                ResultCallback = HandleStockFormUpdate,
            });
        }

        private async Task HandleStockFormUpdate(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(result.Message, sessionService!.AppTitle);
                if (result.RefreshGrid)
                {
                    await LoadStockData();

                    // ✅ TRIGGER CASHFLOW RECALCULATION
                    Logger?.LogInformation("Triggering cashflow recalculation after stock save for assessment {AssessmentId}", AssessmentId);
                    await projectionStateManager!.InvalidateDataAsync("stock", AssessmentId, AssessmentId);
                }
            }
            else
            {
                _Toast!.ShowError(result.Message, sessionService!.AppTitle);
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
                    Logger?.LogDebug("AssessmentStockPage unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing AssessmentStockPage");
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}