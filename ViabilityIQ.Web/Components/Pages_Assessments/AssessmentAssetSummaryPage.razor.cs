using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAssetSummaryPage : ComponentBase, IAsyncDisposable
{
    [Inject] private MasterDataService ViqCrudService { get; set; } = default!;
    [Inject] private ISessionService? SessionService { get; set; }
    [Inject] private ZabOffCanvasService ZabCanvasService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentAssetSummaryPage> Logger { get; set; } = default!;

    [Parameter, EditorRequired] public long AssessmentId { get; set; }

    private readonly List<AssessmentAssetDto> assetsList = new();
    private bool IsLoading { get; set; } = true;
    private bool IsPrinting { get; set; }
    private bool ShowAlertsContainer { get; set; } = true;
    private string SearchQuery { get; set; } = string.Empty;
    private long SelectedFilterId { get; set; }
    private string currentSortColumn = "AssetName";
    private bool isAscending = true;
    private long? LoadedAssessmentId { get; set; }
    private List<SummaryAlert> SummaryAlerts { get; set; } = new();

    private int TotalAssetsCount => FilteredAndSortedAssets.Count();
    private decimal TotalGrossValue => FilteredAndSortedAssets.Sum(x => x.OpeningNetBookValue);
    private decimal TotalAdditions => FilteredAndSortedAssets.Sum(x => x.TotalAdditions);
    private decimal TotalDisposals => FilteredAndSortedAssets.Sum(x => x.TotalDisposals);
    private decimal TotalDepreciationExpense => FilteredAndSortedAssets.Sum(x => x.TotalDepreciation);
    private decimal totalAccumulatedDepreciation => FilteredAndSortedAssets.Sum(x => x.OpeningAccumulatedDepreciation);
    private decimal TotalNetValue => FilteredAndSortedAssets.Sum(x => x.ClosingNetBookValue);

    private IEnumerable<AssessmentAssetDto> FilteredAndSortedAssets
    {
        get
        {
            var query = assetsList.Where(x =>
                (string.IsNullOrWhiteSpace(SearchQuery) ||
                 (x.AssetName != null && x.AssetName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))) &&
                (SelectedFilterId == 0 || x.AssetCategoryId == SelectedFilterId));

            return currentSortColumn switch
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
        }
    }

    protected override void OnInitialized()
    {
        ProjectionStateManager.ProjectionChanged += OnProjectionChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (AssessmentId <= 0)
        {
            assetsList.Clear();
            LoadedAssessmentId = null;
            IsLoading = false;
            return;
        }

        if (LoadedAssessmentId != AssessmentId)
        {
            await LoadAndMapAssetsData();
        }
    }

    private async Task LoadAndMapAssetsData()
    {
        IsLoading = true;

        try
        {
            var result = await ViqCrudService.GetListAsync<AssessmentAssetDto>(
                "vw_assessment_asset_list",
                new { AssessmentId },
                "AssessmentId");

            assetsList.Clear();
            if (result != null)
            {
                assetsList.AddRange(result);
            }

            SummaryAlerts = BuildSummaryAlerts();
            ShowAlertsContainer = true;
            LoadedAssessmentId = AssessmentId;
            Logger.LogInformation(
                "Loaded {AssetCount} summary assets for assessment {AssessmentId}",
                assetsList.Count,
                AssessmentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading summary assets for assessment {AssessmentId}", AssessmentId);
            Toast.ShowError(ex.Message, SessionService?.AppTitle);
        }
        finally
        {
            IsLoading = false;
        }
    }

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
        if (currentSortColumn != columnName)
        {
            return "bi bi-arrow-down-up text-muted opacity-50";
        }

        return isAscending ? "bi bi-arrow-up text-primary" : "bi bi-arrow-down text-primary";
    }

    private Task AddAsset() => OpenAssetFormPanel(new AssessmentAssetDto());

    private async Task PrintPageAsync()
    {
        if (IsPrinting)
        {
            return;
        }

        try
        {
            IsPrinting = true;
            await JS.InvokeVoidAsync("viqAssetDetailsCharts.printExpandedSummary");
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Could not print the asset summary page");
            Toast.ShowError("The print preview could not be opened.", SessionService?.AppTitle);
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private void DismissAlerts()
    {
        ShowAlertsContainer = false;
    }

    private List<SummaryAlert> BuildSummaryAlerts()
    {
        var alerts = new List<SummaryAlert>(3);

        if (assetsList.Count == 0)
        {
            alerts.Add(new(
                "warning",
                "bi bi-exclamation-triangle-fill",
                "No assets recorded",
                "Add the assessment's assets so depreciation and cashflow projections include the asset base."));
            return alerts;
        }

        var impairedCount = assetsList.Count(asset => asset.IsImpaired);
        if (impairedCount > 0)
        {
            alerts.Add(new(
                "danger",
                "bi bi-shield-exclamation",
                $"{impairedCount} impaired asset{(impairedCount == 1 ? string.Empty : "s")}",
                "Review impairment values because they reduce the projected carrying value and may affect funding decisions."));
        }

        var fullyDepreciatedCount = assetsList.Count(asset => asset.ClosingNetBookValue <= 0);
        if (fullyDepreciatedCount > 0)
        {
            alerts.Add(new(
                "warning",
                "bi bi-hourglass-bottom",
                $"{fullyDepreciatedCount} fully depreciated asset{(fullyDepreciatedCount == 1 ? string.Empty : "s")}",
                "Confirm whether these assets remain operational or require replacement expenditure in the cashflow forecast."));
        }

        var openingValue = assetsList.Sum(asset => asset.OpeningNetBookValue);
        var disposals = assetsList.Sum(asset => Math.Abs(asset.TotalDisposals));
        if (openingValue > 0 && disposals / openingValue >= 0.20m)
        {
            alerts.Add(new(
                "warning",
                "bi bi-arrow-down-right-circle-fill",
                "Material disposal pressure",
                $"Planned disposals equal {(disposals / openingValue):P0} of opening net book value; review replacement and cashflow assumptions."));
        }

        var depreciation = assetsList.Sum(asset => asset.TotalDepreciation);
        if (alerts.Count < 3 && openingValue > 0 && depreciation / openingValue >= 0.20m)
        {
            alerts.Add(new(
                "info",
                "bi bi-graph-down-arrow",
                "High depreciation burden",
                $"Projected depreciation equals {(depreciation / openingValue):P0} of opening net book value and materially reduces closing asset value."));
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new(
                "success",
                "bi bi-check-circle-fill",
                "Asset position is stable",
                "No material impairment, fully depreciated assets, or asset-value pressure was detected."));
        }

        return alerts.Take(3).ToList();
    }

    private async Task OpenAssetFormPanel(AssessmentAssetDto asset)
    {
        await ZabCanvasService.ShowAsync(new CanvasRequest
        {
            Title = asset.AssessmentAssetId == 0 ? "Add Asset" : "Edit Asset",
            Width = 400,
            ComponentType = typeof(AssessmentAssetFormComponent),
            Parameters = new Dictionary<string, object>
            {
                ["AssessmentAssetId"] = asset.AssessmentAssetId,
                ["AssessmentId"] = AssessmentId
            },
            ResultCallback = OnSaveComplete
        });
    }

    private async Task ViewAssetMovements(long assetId, string? assetName)
    {
        try
        {
            await ZabCanvasService.ShowAsync(new CanvasRequest
            {
                Title = $"Asset Movement Details - {assetName}",
                Width = 800,
                ComponentType = typeof(AssetMovementFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    ["AssessmentAssetId"] = assetId,
                    ["AssessmentId"] = AssessmentId
                },
                ResultCallback = OnSaveComplete
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error opening asset movements");
            Toast.ShowError($"Error: {ex.Message}", "Error");
        }
    }

    private async Task OnSaveComplete(SaveResult result)
    {
        if (result.Success)
        {
            Toast.ShowSuccess(result.Message, SessionService?.AppTitle);
            if (result.RefreshGrid)
            {
                await LoadAndMapAssetsData();
                await ProjectionStateManager.InvalidateDataAsync("assets", AssessmentId, AssessmentId);
            }
        }
        else
        {
            Toast.ShowError(result.Message, SessionService?.AppTitle);
        }
    }

    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs args)
    {
        if (args.AssessmentId == AssessmentId)
        {
            _ = InvokeAsync(LoadAndMapAssetsData);
        }
    }

    private static string GetAssetStatusText(AssessmentAssetDto asset)
    {
        if (asset.IsImpaired) return "Impaired";
        if (asset.ClosingNetBookValue <= 0) return "Fully Depreciated";
        if (asset.IsDepreciable && asset.DepreciationRate > 0) return "Active";
        return "Stable";
    }

    private static string GetAssetStatusBadgeClass(AssessmentAssetDto asset)
    {
        if (asset.IsImpaired) return "bg-danger text-white";
        if (asset.ClosingNetBookValue <= 0) return "bg-secondary text-white";
        return "bg-success text-white";
    }

    public ValueTask DisposeAsync()
    {
        ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;
        return ValueTask.CompletedTask;
    }

    private sealed record SummaryAlert(
        string Severity,
        string Icon,
        string Title,
        string Message);
}
