using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAssetDetailsPage : ComponentBase, IAsyncDisposable
{
    private const string TrendChartId = "aad-portfolio-trend-chart";
    private const string MovementChartId = "aad-movement-composition-chart";
    private const string DistributionChartId = "aad-closing-distribution-chart";

    [Inject] private MasterDataService MasterDataService { get; set; } = default!;
    [Inject] private IAssetRepository AssetRepository { get; set; } = default!;
    [Inject] private ISessionService? sessionService { get; set; }
    [Inject] private ZabOffCanvasService OffCanvasService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentAssetDetailsPage> Logger { get; set; } = default!;

    [Parameter, EditorRequired] public long AssessmentId { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsPrinting { get; set; }
    private bool ShowAllAlerts { get; set; }
    private bool ShowAlertsContainer { get; set; } = true;
    private bool AssetNameSortAscending { get; set; } = true;
    private bool RenderChartsAfterLoad { get; set; }
    private string? PendingPrintMode { get; set; }
    private HashSet<long>? ExpandedAssetIdsBeforePrint { get; set; }
    private long? LoadedAssessmentId { get; set; }
    private string _searchQuery = string.Empty;
    private long _selectedCategoryId;

    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value)
            {
                return;
            }

            _searchQuery = value;
            RenderChartsAfterLoad = true;
        }
    }

    private long SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (_selectedCategoryId == value)
            {
                return;
            }

            _selectedCategoryId = value;
            RenderChartsAfterLoad = true;
        }
    }
    private ZabConfirmDialogComponent? ConfirmDeactivateDialog { get; set; }
    private HashSet<long> ExpandedAssetIds { get; } = new();
    private List<AssetDashboardRow> Assets { get; set; } = new();
    private List<AssetAlertItem> AssetAlerts { get; set; } = new();

    private IEnumerable<AssetDashboardRow> FilteredAssets
    {
        get
        {
            var query = Assets.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(asset =>
                    asset.AssetName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    asset.AssetCategoryName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    asset.AssetTypeName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedCategoryId > 0)
            {
                query = query.Where(asset => asset.AssetCategoryId == SelectedCategoryId);
            }

            return AssetNameSortAscending
                ? query.OrderBy(asset => asset.AssetName)
                : query.OrderByDescending(asset => asset.AssetName);
        }
    }

    private IEnumerable<AssetAlertItem> VisibleAlerts =>
        ShowAllAlerts ? AssetAlerts : Enumerable.Empty<AssetAlertItem>();

    private int TotalAssetsCount => FilteredAssets.Count();
    private int FilteredAssetCount => FilteredAssets.Count();
    private decimal TotalOpeningNetBookValue => FilteredAssets.Sum(asset => asset.OpeningNetBookValue);
    private decimal TotalAdditions => FilteredAssets.Sum(asset => asset.TotalAdditions);
    private decimal TotalDisposals => FilteredAssets.Sum(asset => asset.TotalDisposals);
    private decimal TotalAccumulatedDepreciation => FilteredAssets.Sum(asset => asset.ClosingAccumulatedDepreciation);
    private decimal TotalClosingNetBookValue => FilteredAssets.Sum(asset => asset.ClosingNetBookValue);
    private decimal TotalMovementVolume => FilteredAssets.Sum(asset =>
        asset.Months.Sum(month => Math.Abs(month.SignedMovementValue)));
    private string AppTitle => sessionService?.AppTitle ?? "Viability.IQ";

    protected override void OnInitialized()
    {
        ProjectionStateManager.ProjectionChanged += OnProjectionChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (AssessmentId <= 0)
        {
            LoadedAssessmentId = null;
            Assets = new();
            AssetAlerts = new();
            IsLoading = false;
            return;
        }

        if (LoadedAssessmentId != AssessmentId)
        {
            await LoadAssetsAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (RenderChartsAfterLoad && !IsLoading)
        {
            RenderChartsAfterLoad = false;
            await RenderChartsAsync();
        }

        if (PendingPrintMode != null)
        {
            await OpenPrintDialogAsync();
        }
    }

    private async Task LoadAssetsAsync()
    {
        try
        {
            IsLoading = true;

            var dtoTask = MasterDataService.GetListAsync<AssessmentAssetDto>(
                "vw_assessment_asset_list",
                new { AssessmentId },
                "AssetName");
            var assetsTask = AssetRepository.GetAssessmentAssetsAsync(AssessmentId);
            var movementsTask = AssetRepository.GetAllAssetMovementsAsync(AssessmentId);

            await Task.WhenAll(dtoTask, assetsTask, movementsTask);

            var assetDtos = (await dtoTask)
                .GroupBy(item => item.AssessmentAssetId)
                .ToDictionary(group => group.Key, group => group.First());
            var assetMasters = await assetsTask;
            var movementLookup = (await movementsTask)
                .Where(item => item.Active)
                .GroupBy(item => item.AssessmentAssetId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.ModifiedDate).First());

            Assets = assetMasters
                .Where(asset => asset.Active)
                .Select(asset =>
                {
                    assetDtos.TryGetValue(asset.AssessmentAssetId, out var dto);
                    movementLookup.TryGetValue(asset.AssessmentAssetId, out var movement);
                    return BuildDashboardRow(asset, dto, movement);
                })
                .OrderBy(asset => asset.AssetName)
                .ToList();

            AssetAlerts = BuildAlerts(Assets);
            ShowAlertsContainer = true;
            LoadedAssessmentId = AssessmentId;

            foreach (var assetId in ExpandedAssetIds.Where(id => Assets.All(asset => asset.AssessmentAssetId != id)).ToList())
            {
                ExpandedAssetIds.Remove(assetId);
            }

            if (ExpandedAssetIds.Count == 0 && Assets.Count > 0)
            {
                ExpandedAssetIds.Add(Assets[0].AssessmentAssetId);
            }

            RenderChartsAfterLoad = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading asset detail dashboard for assessment {AssessmentId}", AssessmentId);
            Toast.ShowError($"Could not load asset details: {ex.Message}", AppTitle);
            Assets = new();
            AssetAlerts = new();
            LoadedAssessmentId = null;
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static AssetDashboardRow BuildDashboardRow(
        AssessmentAsset asset,
        AssessmentAssetDto? dto,
        AssessmentAssetMovement? movement)
    {
        var months = new List<AssetMonthValue>(12);
        decimal previousClosingNetBookValue = asset.OpeningNetBookValue;

        for (var month = 1; month <= 12; month++)
        {
            var isActive = asset.IsActiveInMonth(month);
            var isAcquisitionMonth = asset.IsPreExisting ? month == 1 : month == asset.AcquisitionStartMonth;

            if (!isActive)
            {
                months.Add(AssetMonthValue.Inactive(month));
                continue;
            }

            var openingNetBookValue = isAcquisitionMonth
                ? asset.OpeningNetBookValue
                : previousClosingNetBookValue;
            var movementType = movement?.GetMovementType(month);
            var rawMovementValue = movement?.GetMovementValue(month) ?? 0m;
            var signedMovementValue = GetSignedMovementValue(movementType, rawMovementValue);
            var depreciation = movement?.GetDepreciation(month) ?? 0m;
            var storedClosing = movement?.GetNetBookValue(month);
            var calculatedClosing = Math.Max(0m, openingNetBookValue + signedMovementValue - depreciation);
            var closingNetBookValue = storedClosing ?? calculatedClosing;

            months.Add(new AssetMonthValue(
                month,
                true,
                isAcquisitionMonth,
                openingNetBookValue,
                movementType,
                signedMovementValue,
                depreciation,
                closingNetBookValue));

            previousClosingNetBookValue = closingNetBookValue;
        }

        return new AssetDashboardRow(
            asset.AssessmentAssetId,
            asset.AssetName ?? "Unnamed Asset",
            dto?.AssetCategoryId ?? asset.AssetCategoryId,
            dto?.AssetCategoryName ?? "Uncategorised",
            dto?.AssetTypeName ?? "Unspecified",
            asset.IsPreExisting,
            asset.AcquisitionStartMonth,
            asset.IsDepreciable,
            asset.DepreciationRate,
            asset.DepreciationMethod,
            asset.IsImpaired,
            asset.OpeningNetBookValue,
            months.LastOrDefault(month => month.IsActive)?.ClosingNetBookValue ?? 0m,
            months.LastOrDefault(month => month.IsActive)?.ClosingAccumulatedDepreciation(movement) ?? 0m,
            months.Where(month => month.MovementType == "Addition").Sum(month => Math.Abs(month.SignedMovementValue)),
            months.Where(month => month.MovementType == "Disposal").Sum(month => Math.Abs(month.SignedMovementValue)),
            movement != null,
            months);
    }

    private static decimal GetSignedMovementValue(string? movementType, decimal movementValue)
    {
        if (string.IsNullOrWhiteSpace(movementType))
        {
            return 0m;
        }

        return movementType switch
        {
            "Disposal" => -Math.Abs(movementValue),
            "Addition" => Math.Abs(movementValue),
            "Transfer" => 0m,
            _ => movementValue
        };
    }

    private static List<AssetAlertItem> BuildAlerts(IEnumerable<AssetDashboardRow> assets)
    {
        var alerts = new List<AssetAlertItem>();

        foreach (var asset in assets)
        {
            if (asset.IsImpaired)
            {
                alerts.Add(new(asset.AssessmentAssetId, "danger", "bi bi-exclamation-octagon",
                    $"{asset.AssetName} is impaired",
                    "Review the impairment reason and confirm that its carrying value is current."));
            }

            if (asset.ClosingNetBookValue <= 0)
            {
                alerts.Add(new(asset.AssessmentAssetId, "warning", "bi bi-hourglass-bottom",
                    $"{asset.AssetName} is fully depreciated",
                    "The asset remains active but has no closing net book value."));
            }

            if (!asset.HasMovementRecord)
            {
                alerts.Add(new(asset.AssessmentAssetId, "warning", "bi bi-table",
                    $"{asset.AssetName} has no movement record",
                    "Create its monthly movement schedule before relying on portfolio projections."));
            }

            if (asset.IsDepreciable &&
                (asset.DepreciationRate <= 0 || string.IsNullOrWhiteSpace(asset.DepreciationMethod)))
            {
                alerts.Add(new(asset.AssessmentAssetId, "warning", "bi bi-sliders",
                    $"{asset.AssetName} has incomplete depreciation settings",
                    "A depreciable asset requires both a positive rate and a depreciation method."));
            }

            if (!asset.IsPreExisting)
            {
                var acquisitionMonth = asset.Months.FirstOrDefault(month => month.IsAcquisitionMonth);
                if (acquisitionMonth == null || acquisitionMonth.OpeningNetBookValue <= 0)
                {
                    alerts.Add(new(asset.AssessmentAssetId, "danger", "bi bi-calendar-x",
                        $"{asset.AssetName} has no acquisition-month value",
                        $"Month {asset.AcquisitionStartMonth} is marked as the acquisition month but has no opening value."));
                }
            }
        }

        return alerts
            .OrderBy(alert => alert.Severity == "danger" ? 0 : 1)
            .ThenBy(alert => alert.Title)
            .ToList();
    }

    private void ToggleAsset(long assessmentAssetId)
    {
        if (!ExpandedAssetIds.Add(assessmentAssetId))
        {
            ExpandedAssetIds.Remove(assessmentAssetId);
        }
    }

    private void ToggleAlerts() => ShowAllAlerts = !ShowAllAlerts;

    private void DismissAlerts() => ShowAlertsContainer = false;

    private void ToggleAssetNameSort()
    {
        AssetNameSortAscending = !AssetNameSortAscending;
    }

    private Task PrintSummaryAsync() => PreparePrintAsync(includeDetails: false);

    private Task PrintDetailedAsync() => PreparePrintAsync(includeDetails: true);

    private Task PreparePrintAsync(bool includeDetails)
    {
        if (IsPrinting)
        {
            return Task.CompletedTask;
        }

        ExpandedAssetIdsBeforePrint = ExpandedAssetIds.ToHashSet();
        IsPrinting = true;
        PendingPrintMode = includeDetails ? "detailed" : "summary";
        ExpandedAssetIds.Clear();

        if (includeDetails)
        {
            ExpandedAssetIds.UnionWith(FilteredAssets.Select(asset => asset.AssessmentAssetId));
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OpenPrintDialogAsync()
    {
        var printMode = PendingPrintMode;
        PendingPrintMode = null;

        try
        {
            await JS.InvokeVoidAsync("viqAssetDetailsCharts.print", printMode);
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Unable to open the browser print dialog");
            Toast.ShowError("The print preview could not be opened.", AppTitle);
        }
        finally
        {
            ExpandedAssetIds.Clear();

            if (ExpandedAssetIdsBeforePrint != null)
            {
                ExpandedAssetIds.UnionWith(ExpandedAssetIdsBeforePrint);
            }

            ExpandedAssetIdsBeforePrint = null;
            IsPrinting = false;
            StateHasChanged();
        }
    }

    private async Task OpenAssetDetailsAsync(long assessmentAssetId)
    {
        var asset = Assets.First(item => item.AssessmentAssetId == assessmentAssetId);

        await OffCanvasService.ShowAsync(new CanvasRequest
        {
            Title = $"Asset Details - {asset.AssetName}",
            Width = 900,
            ComponentType = typeof(AssessmentAssetDetailComponent),
            Parameters = new Dictionary<string, object>
            {
                ["AssessmentId"] = AssessmentId,
                ["AssessmentAssetId"] = assessmentAssetId
            }
        });
    }

    private async Task AddAssetAsync()
    {
        await OffCanvasService.ShowAsync(new CanvasRequest
        {
            Title = "Add Asset",
            Width = 400,
            ComponentType = typeof(AssessmentAssetFormComponent),
            Parameters = new Dictionary<string, object>
            {
                ["AssessmentAssetId"] = 0L,
                ["AssessmentId"] = AssessmentId
            },
            ResultCallback = HandleMovementResultAsync
        });
    }

    private async Task OpenMovementEditorAsync(AssetDashboardRow asset)
    {
        await OffCanvasService.ShowAsync(new CanvasRequest
        {
            Title = $"Asset Movement Details - {asset.AssetName}",
            Width = 800,
            ComponentType = typeof(AssetMovementFormComponent),
            Parameters = new Dictionary<string, object>
            {
                ["AssessmentAssetId"] = asset.AssessmentAssetId,
                ["AssessmentId"] = AssessmentId
            },
            ResultCallback = HandleMovementResultAsync
        });
    }

    private async Task DeactivateAssetAsync(AssetDashboardRow asset)
    {
        if (ConfirmDeactivateDialog == null)
        {
            Toast.ShowError("The confirmation dialog is unavailable.", AppTitle);
            return;
        }

        var confirmed = await ConfirmDeactivateDialog.ShowAsync(
            title: "Deactivate Asset?",
            message: $"Deactivate {asset.AssetName} and its monthly movement record?",
            confirmText: "Yes, Deactivate",
            cancelText: "No, Keep It");

        if (!confirmed)
        {
            return;
        }

        try
        {
            const string sql = """
                UPDATE tblAssessmentAssetMovement
                SET Active = @Active, ModifiedDate = @ModifiedDate, ModifiedBy = @ModifiedBy
                WHERE AssessmentAssetId = @AssessmentAssetId;

                UPDATE tblAssessmentAssets
                SET Active = @Active, ModifiedDate = @ModifiedDate, ModifiedBy = @ModifiedBy
                WHERE AssessmentAssetId = @AssessmentAssetId;
                """;

            await MasterDataService.ExecuteCommandAsync(sql, new
            {
                Active = false,
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy = sessionService?.UserId ?? 0,
                asset.AssessmentAssetId
            });

            await ProjectionStateManager.InvalidateDataAsync("assets", AssessmentId, AssessmentId);
            Toast.ShowSuccess($"{asset.AssetName} was deactivated.", AppTitle);
            await LoadAssetsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deactivating asset {AssessmentAssetId}", asset.AssessmentAssetId);
            Toast.ShowError($"Could not deactivate the asset: {ex.Message}", AppTitle);
        }
    }

    private async Task HandleMovementResultAsync(SaveResult result)
    {
        if (result.Success)
        {
            Toast.ShowSuccess(result.Message, AppTitle);
            await LoadAssetsAsync();
            await ProjectionStateManager.InvalidateDataAsync("assets", AssessmentId, AssessmentId);
        }
        else if (!result.Cancelled)
        {
            Toast.ShowError(result.Message, AppTitle);
        }
    }

    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs args)
    {
        if (args.AssessmentId == AssessmentId)
        {
            _ = InvokeAsync(LoadAssetsAsync);
        }
    }

    private async Task RenderChartsAsync()
    {
        try
        {
            var trendValues = Enumerable.Range(1, 12)
                .Select(month => FilteredAssets.Sum(asset =>
                    asset.Months.First(item => item.Month == month).ClosingNetBookValue))
                .ToArray();

            var movementValues = new[]
            {
                SumMovement("Addition"),
                SumMovement("Disposal"),
                SumMovement("Revaluation"),
                SumMovement("Transfer")
            };

            var distribution = FilteredAssets
                .OrderByDescending(asset => asset.ClosingNetBookValue)
                .Take(10)
                .ToList();

            await JS.InvokeVoidAsync(
                "viqAssetDetailsCharts.render",
                new
                {
                    trend = new
                    {
                        canvasId = TrendChartId,
                        labels = Enumerable.Range(1, 12).Select(month => $"M{month}").ToArray(),
                        values = trendValues
                    },
                    movements = new
                    {
                        canvasId = MovementChartId,
                        labels = new[] { "Additions", "Disposals", "Revaluations", "Transfers" },
                        values = movementValues
                    },
                    distribution = new
                    {
                        canvasId = DistributionChartId,
                        labels = distribution.Select(asset => asset.AssetName).ToArray(),
                        values = distribution.Select(asset => asset.ClosingNetBookValue).ToArray()
                    }
                });
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Asset charts could not be rendered");
        }
    }

    private decimal SumMovement(string movementType) =>
        FilteredAssets.Sum(asset => asset.Months
            .Where(month => month.MovementType == movementType)
            .Sum(month => Math.Abs(month.SignedMovementValue)));

    private static string GetMonthCellClass(AssetMonthValue month) =>
        month.IsActive ? "aad-month-value" : "aad-month-value aad-pre-acquisition";

    private static string GetAssetIcon(AssetDashboardRow asset)
    {
        var category = asset.AssetCategoryName.ToLowerInvariant();
        if (category.Contains("vehicle")) return "bi bi-truck";
        if (category.Contains("property") || category.Contains("building")) return "bi bi-buildings";
        if (category.Contains("computer") || category.Contains("technology")) return "bi bi-pc-display";
        if (category.Contains("plant") || category.Contains("machinery")) return "bi bi-gear-wide-connected";
        return "bi bi-box-seam";
    }

    private static string GetStatusText(AssetDashboardRow asset)
    {
        if (asset.IsImpaired) return "Impaired";
        if (asset.ClosingNetBookValue <= 0) return "Fully Depreciated";
        return asset.HasMovementRecord ? "Active" : "Needs Setup";
    }

    private static string GetStatusCssClass(AssetDashboardRow asset)
    {
        if (asset.IsImpaired) return "aad-status-danger";
        if (asset.ClosingNetBookValue <= 0) return "aad-status-muted";
        return asset.HasMovementRecord ? "aad-status-success" : "aad-status-warning";
    }

    private static string GetMovementAmountClass(string? movementType) =>
        movementType == "Disposal" ? "aad-movement-negative" : "aad-movement-positive";

    private static string GetMovementBadgeClass(string? movementType) =>
        $"aad-movement-badge aad-movement-{movementType?.ToLowerInvariant() ?? "none"}";

    private static string GetMovementAbbreviation(string? movementType) =>
        movementType switch
        {
            "Addition" => "ADD",
            "Disposal" => "DSP",
            "Revaluation" => "REV",
            "Transfer" => "TRF",
            _ => "—"
        };

    private static string FormatAmount(decimal value) =>
        value == 0 ? "0" : value.ToString("N0").Replace(",", " ");

    private static string FormatSignedAmount(decimal value) =>
        value > 0
            ? $"+{FormatAmount(value)}"
            : value < 0
                ? $"-{FormatAmount(Math.Abs(value))}"
                : "0";

    private static string FormatCurrency(decimal value) => $"R {FormatAmount(value)}";

    public async ValueTask DisposeAsync()
    {
        ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;

        try
        {
            await JS.InvokeVoidAsync("viqAssetDetailsCharts.destroy");
        }
        catch (JSDisconnectedException)
        {
            // The browser circuit is already gone; no chart cleanup is required.
        }
        catch (JSException ex)
        {
            Logger.LogDebug(ex, "Asset chart cleanup was unavailable");
        }
    }

    private sealed record AssetDashboardRow(
        long AssessmentAssetId,
        string AssetName,
        long? AssetCategoryId,
        string AssetCategoryName,
        string AssetTypeName,
        bool IsPreExisting,
        int AcquisitionStartMonth,
        bool IsDepreciable,
        decimal DepreciationRate,
        string? DepreciationMethod,
        bool IsImpaired,
        decimal OpeningNetBookValue,
        decimal ClosingNetBookValue,
        decimal ClosingAccumulatedDepreciation,
        decimal TotalAdditions,
        decimal TotalDisposals,
        bool HasMovementRecord,
        List<AssetMonthValue> Months);

    private sealed record AssetMonthValue(
        int Month,
        bool IsActive,
        bool IsAcquisitionMonth,
        decimal OpeningNetBookValue,
        string? MovementType,
        decimal SignedMovementValue,
        decimal Depreciation,
        decimal ClosingNetBookValue)
    {
        public bool HasMovement =>
            !string.IsNullOrWhiteSpace(MovementType) &&
            (SignedMovementValue != 0 || MovementType == "Transfer");

        public decimal ClosingAccumulatedDepreciation(AssessmentAssetMovement? movement) =>
            movement?.GetAccumulatedDepreciation(Month) ?? 0m;

        public static AssetMonthValue Inactive(int month) =>
            new(month, false, false, 0m, null, 0m, 0m, 0m);
    }

    private sealed record AssetAlertItem(
        long AssessmentAssetId,
        string Severity,
        string Icon,
        string Title,
        string Message);
}
