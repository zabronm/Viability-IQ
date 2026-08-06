using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockPage : ComponentBase, IAsyncDisposable
    {
        #region Injected Services

        [Inject] private IGenericDataRepository<AssessmentStock> DataRepository { get; set; } = default!;
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] MasterDataService? ViqCrudService { get; set; }
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
        private List<AssessmentStockDto> StockDataList = new();
        private List<UnifiedStockViewModel> FilteredStockList = new();
        private bool IsLoading = false;
        private string SearchTerm = string.Empty;

        #endregion

        #region Properties

        private decimal GrandTotalStockValue => FilteredStockList?.Sum(x => x.MonthlyValues.Sum()) ?? 0;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AssessmentId = sessionService?.AssessmentId ?? 0;

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
                _Toast?.ShowError(ex.Message, sessionService?.AppTitle);
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadStockData()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug("Loading stock data for assessment {AssessmentId}", AssessmentId);

                StockDataList = (await ViqCrudService!.GetListAsync<AssessmentStockDto>("vw_assessment_stock_list",
                    new { AssessmentId = sessionService!.AssessmentId }, "AssessmentId"))?.ToList() ?? new();

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
            var sourceList = string.IsNullOrWhiteSpace(SearchTerm)
                ? StockDataList
                : StockDataList.Where(x => x.AssessmentSalesCategoryName!.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            FilteredStockList = sourceList.Select(dto => new UnifiedStockViewModel
            {
                Id = dto.AssessmentStockId,
                AssessmentSalesCategoryName = dto.AssessmentSalesCategoryName ?? "Unknown",
                Description = dto.Description ?? "",
                blIncludeVAT = dto.blIncludeVAT,
                MonthlyValues = new decimal[] {
                    dto.Month_1, dto.Month_2, dto.Month_3, dto.Month_4, dto.Month_5, dto.Month_6,
                    dto.Month_7, dto.Month_8, dto.Month_9, dto.Month_10, dto.Month_11, dto.Month_12
                }
            }).ToList();
        }

        private decimal GetMonthValue(UnifiedStockViewModel item, int month) => item.MonthlyValues[month - 1];

        private decimal GetTotalForMonth(int month) => FilteredStockList.Sum(x => x.MonthlyValues[month - 1]);

        private decimal GetGrandTotalStockValue() => FilteredStockList.Sum(x => x.MonthlyValues.Sum());

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
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
                Title = stockId == 0 ? "Add Monthly Stock Movement" : "Edit Monthly Stock Movement",
                ComponentType = typeof(AssessmentStockFormComponent),
                Width = 350,
                Parameters = new
                {
                    AssessmentId = sessionService!.AssessmentId,
                    AssessmentStockId = stockId,
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