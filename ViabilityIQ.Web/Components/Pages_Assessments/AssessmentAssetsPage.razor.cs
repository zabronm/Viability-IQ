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
using ViabilityIQ.Shared.UtilityServices;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    /// <summary>
    /// AssessmentAssetsPage - Display and manage assets using AssessmentAssetDto with sorting and live totals
    /// </summary>
    public partial class AssessmentAssetsPage : ComponentBase, IAsyncDisposable
    {
        #region Injected Services
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] ILogger<AssessmentAssetsPage>? Logger { get; set; }
        [Inject] private IGenericDataRepository<AssessmentAsset> coreAssetRepository { get; set; } = default!;
        #endregion

        #region Parameters
        [Parameter] public long AssessmentId { get; set; }
        #endregion

        #region Private Fields - Data & State
        private List<AssessmentAssetDto> assetsList = new();
        private bool IsLoading { get; set; } = true;
        private string SearchQuery { get; set; } = string.Empty;
        private long SelectedFilterId { get; set; } = 0;

        // Sorting state
        private string currentSortColumn = "AssetName";
        private bool isAscending = true;
        #endregion

        #region Dynamic Summary Totals Properties (Reflects Filtered View)
        private int TotalAssetsCount => FilteredAndSortedAssets.Count();
        private decimal TotalGrossValue => FilteredAndSortedAssets.Sum(x => x.OpeningNetBookValue);
        private decimal TotalAdditions => FilteredAndSortedAssets.Sum(x => x.TotalAdditions);
        private decimal TotalDisposals => FilteredAndSortedAssets.Sum(x => x.TotalDisposals);
        private decimal TotalDepreciationExpense => FilteredAndSortedAssets.Sum(x => x.TotalDepreciation);
        private decimal totalAccumulatedDepreciation => FilteredAndSortedAssets.Sum(x => x.OpeningAccumulatedDepreciation);
        private decimal TotalNetValue => FilteredAndSortedAssets.Sum(x => x.ClosingNetBookValue);
        #endregion

        #region Filtered & Sorted Assets Property
        private IEnumerable<AssessmentAssetDto> FilteredAndSortedAssets
        {
            get
            {
                var query = assetsList.Where(x =>
                    (string.IsNullOrWhiteSpace(SearchQuery) || (x.AssetName != null && x.AssetName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))) &&
                    (SelectedFilterId == 0 || x.AssetCategoryId == SelectedFilterId));

                query = currentSortColumn switch
                {
                    "Category" => isAscending ? query.OrderBy(x => x.AssetCategoryName) : query.OrderByDescending(x => x.AssetCategoryName),
                    "Type" => isAscending ? query.OrderBy(x => x.AssetTypeName) : query.OrderByDescending(x => x.AssetTypeName),
                    "OpeningValue" => isAscending ? query.OrderBy(x => x.OpeningNetBookValue) : query.OrderByDescending(x => x.OpeningNetBookValue),
                    "Additions" => isAscending ? query.OrderBy(x => x.TotalAdditions) : query.OrderByDescending(x => x.TotalAdditions),
                    "Disposals" => isAscending ? query.OrderBy(x => x.TotalDisposals) : query.OrderByDescending(x => x.TotalDisposals),
                    "Depreciation" => isAscending ? query.OrderBy(x => x.TotalDepreciation) : query.OrderByDescending(x => x.TotalDepreciation),
                    "NetBookValue" => isAscending ? query.OrderBy(x => x.ClosingNetBookValue) : query.OrderByDescending(x => x.ClosingNetBookValue),
                    _ => isAscending ? query.OrderBy(x => x.AssetName) : query.OrderByDescending(x => x.AssetName)
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
                AssessmentId = sessionService?.AssessmentId ?? 0;
                Logger?.LogInformation("AssessmentAssetsPage initialized for assessment {AssessmentId}", AssessmentId);

                await LoadAndMapAssetsData();
                IsLoading = false;

                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged += OnProjectionChanged;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing AssessmentAssetsPage");
                _Toast?.ShowError(ex.Message);
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

        #region Private Methods - Data Loading
        private async Task LoadAndMapAssetsData()
        {
            try
            {
                Logger?.LogDebug("Loading assets data for assessment {AssessmentId}", AssessmentId);

                var result = await ViqCrudService!.GetListAsync<AssessmentAssetDto>("vw_assessment_asset_list",
                    new { AssessmentId }, "AssessmentId");

                if (result != null)
                {
                    assetsList = result.ToList();
                    Logger?.LogInformation("Loaded {AssetCount} asset DTO records for assessment {AssessmentId}",
                        assetsList.Count, AssessmentId);

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading assets data for assessment {AssessmentId}", AssessmentId);
                _Toast?.ShowError(ex.Message, sessionService?.AppTitle);
            }
        }
        #endregion

        #region Private Methods - UI Interaction
        private async Task AddAsset()
        {
            await OpenAssetFormPanel(new AssessmentAssetDto());
        }

        private async Task OpenAssetFormPanel(AssessmentAssetDto asset)
        {
            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = asset.AssessmentAssetId == 0 ? "Add Asset" : "Edit Asset",
                Width = 400,
                ComponentType = typeof(AssessmentAssetFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssessmentAssetId", asset.AssessmentAssetId },
                    { "AssessmentId", sessionService?.AssessmentId ?? 0 }
                },
                ResultCallback = OnSaveComplete
            });
        }

        private async Task ViewAssetMovements(long assetId, string? assetName)
        {
            try
            {
                await zabCanvasService!.ShowAsync(new CanvasRequest
                {
                    Title = $"Asset Movement Details - {assetName}",
                    Width = 800,
                    ComponentType = typeof(AssetMovementFormComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "AssessmentAssetId", assetId },
                        { "AssessmentId", sessionService?.AssessmentId ?? 0 }
                    },
                    ResultCallback = OnSaveComplete
                });
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error opening asset movements");
                _Toast?.ShowError($"Error: {ex.Message}", "Error");
            }
        }

        private async Task OpenBulkImport()
        {
            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = "Bulk Assets Import",
                Width = 700,
                ComponentType = typeof(BulkAssetsImportComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssessmentId", AssessmentId }
                },
                ResultCallback = OnSaveComplete
            });
        }
        #endregion

        #region Private Methods - Event Callbacks
        private async Task OnSaveComplete(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(result.Message, sessionService!.AppTitle);
                if (result.RefreshGrid)
                {
                    await LoadAndMapAssetsData();
                    await projectionStateManager!.InvalidateDataAsync("assets", AssessmentId, AssessmentId);
                }
            }
            else
            {
                _Toast!.ShowError(result.Message, sessionService!.AppTitle);
            }
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            if (e.AssessmentId == AssessmentId)
            {
                InvokeAsync(async () => await LoadAndMapAssetsData());
            }
        }
        #endregion

        #region UI Helper Methods
        private string GetAssetStatusText(AssessmentAssetDto asset)
        {
            if (asset.IsImpaired) return "Impaired";
            if (asset.ClosingNetBookValue <= 0) return "Fully Depreciated";
            if (asset.IsDepreciable && asset.DepreciationRate > 0) return "Active";
            return "Stable";
        }

        private string GetAssetStatusBadgeClass(AssessmentAssetDto asset)
        {
            if (asset.IsImpaired) return "bg-danger text-white";
            if (asset.ClosingNetBookValue <= 0) return "bg-secondary text-white";
            return "bg-success text-white";
        }
        #endregion

        #region Disposal
        public async ValueTask DisposeAsync()
        {
            if (projectionStateManager != null)
            {
                projectionStateManager.ProjectionChanged -= OnProjectionChanged;
            }
            await Task.CompletedTask;
        }
        #endregion
    }
}