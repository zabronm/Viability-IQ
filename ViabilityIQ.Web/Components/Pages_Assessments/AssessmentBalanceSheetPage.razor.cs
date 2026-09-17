using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentBalanceSheetPage : ComponentBase, IDisposable
{
    [Inject] private ICashflowProjectionService ProjectionService { get; set; } = default!;
    [Inject] private IAssetRepository AssetRepository { get; set; } = default!;
    [Inject] private IAssetEngine AssetEngine { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentBalanceSheetPage> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }

    private CashflowProjectionResult? Projection { get; set; }
    private bool IsLoadingAssets { get; set; } = true;
    private bool IsLoadingProjection { get; set; } = true;
    private string? AssetError { get; set; }
    private string? ProjectionError { get; set; }
    private bool ShowAlerts { get; set; } = true;
    private bool ShowAllAlerts { get; set; }
    private bool IsPrinting { get; set; }
    private bool PrintRequested { get; set; }
    private bool AlertsExpandedBeforePrint { get; set; }
    private long _loadedAssessmentId;

    private int TotalAssetsCount { get; set; }
    private decimal OpeningGrossValue { get; set; }
    private decimal TotalAdditions { get; set; }
    private decimal TotalDisposals { get; set; }
    private decimal ClosingAccumulatedDepreciation { get; set; }
    private decimal ClosingNetBookValue { get; set; }
    private decimal AssetHealthRate =>
        OpeningGrossValue + TotalAdditions - TotalDisposals <= 0m
            ? 0m
            : Math.Clamp(
                ClosingNetBookValue / (OpeningGrossValue + TotalAdditions - TotalDisposals) * 100m,
                0m,
                100m);

    private IReadOnlyList<BalanceSheetAlert> BalanceSheetAlerts { get; set; } = [];
    private string PrimaryAlertIcon => BalanceSheetAlerts.FirstOrDefault()?.Icon ?? "bi-check-circle-fill";
    private string PrimaryAlertSummary => BalanceSheetAlerts.Count switch
    {
        0 => "No material balance-sheet risks were detected.",
        1 => BalanceSheetAlerts[0].Title,
        _ => $"{BalanceSheetAlerts[0].Title} and {BalanceSheetAlerts.Count - 1} additional finding(s)."
    };

    protected override void OnInitialized()
    {
        ProjectionStateManager.ProjectionChanged += OnProjectionChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId != AssessmentId)
        {
            _loadedAssessmentId = AssessmentId;
            await Task.WhenAll(LoadAssetMetricsAsync(), LoadProjectionAsync());
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!PrintRequested)
            return;

        PrintRequested = false;
        await OpenPrintDialogAsync();
    }

    private async Task LoadAssetMetricsAsync()
    {
        IsLoadingAssets = true;
        AssetError = null;

        try
        {
            var assetsTask = AssetRepository.GetAssessmentAssetsAsync(AssessmentId);
            var movementsTask = AssetRepository.GetAllAssetMovementsAsync(AssessmentId);
            var projectionsTask = AssetEngine.GetAllAssetsWithProjectionsAsync(AssessmentId);
            await Task.WhenAll(assetsTask, movementsTask, projectionsTask);

            var assets = await assetsTask;
            var movements = await movementsTask;
            var assetProjections = await projectionsTask;

            TotalAssetsCount = assets.Count;
            OpeningGrossValue = assets.Sum(asset => asset.OpeningBalanceValue);
            TotalAdditions = MovementTotal(movements, "addition");
            TotalDisposals = MovementTotal(movements, "disposal");
            ClosingAccumulatedDepreciation =
                assets.Sum(asset => asset.OpeningAccumulatedDepreciation)
                + assetProjections.Sum(asset => asset.MonthlyDepreciation.Values.Sum());
        }
        catch (Exception ex)
        {
            AssetError = ex.Message;
            Logger.LogError(ex, "Unable to load asset metrics for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoadingAssets = false;
        }
    }

    private async Task LoadProjectionAsync()
    {
        IsLoadingProjection = true;
        ProjectionError = null;

        try
        {
            Projection = await ProjectionService.CalculateAsync(AssessmentId);
            ClosingNetBookValue = Projection.Summary.ClosingFixedAssets;
            BalanceSheetAlerts = BuildAlerts(Projection).Take(6).ToArray();
            ShowAlerts = true;
        }
        catch (Exception ex)
        {
            Projection = null;
            ProjectionError = ex.Message;
            Logger.LogError(ex, "Unable to compile balance sheet for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoadingProjection = false;
        }
    }

    private static decimal MovementTotal(
        IEnumerable<ViabilityIQ.Shared.DataModels.AssessmentAssetMovement> movements,
        string movementName)
    {
        decimal total = 0m;
        foreach (var movement in movements)
        {
            for (var month = 1; month <= 12; month++)
            {
                if (movement.GetMovementType(month)?.Contains(movementName, StringComparison.OrdinalIgnoreCase) == true)
                    total += Math.Abs(movement.GetMovementValue(month));
            }
        }

        return total;
    }

    private static IEnumerable<BalanceSheetAlert> BuildAlerts(CashflowProjectionResult projection)
    {
        var closing = projection.Months.OrderBy(month => month.MonthNumber).Last();
        var alerts = new List<BalanceSheetAlert>();
        var equity = closing.TotalAssets - closing.CurrentLiabilities;
        var overdraftMonths = projection.Months.Count(month => month.ClosingBank < 0m);
        var currentAssetBase = Math.Max(closing.CurrentAssets, 1m);

        if (equity < 0m)
            alerts.Add(Critical("Negative projected net assets", $"Liabilities exceed assets by R {Math.Abs(equity):N0} at M12.", "Prepare a recapitalisation and debt-reduction plan; continued losses or drawings will deepen insolvency risk."));
        else
            alerts.Add(Healthy("Positive net asset position", $"Projected net assets/equity close at R {equity:N0}.", "Protect this capital buffer by retaining sufficient profit and controlling additional borrowing."));

        if (closing.CurrentRatio is < 1m)
            alerts.Add(Critical("Current liabilities exceed current assets", $"The M12 current ratio is {closing.CurrentRatio.GetValueOrDefault():F2}x.", "Accelerate debtor collections, reduce excess stock and negotiate longer supplier terms."));
        else if (closing.CurrentRatio is < 1.5m)
            alerts.Add(Warning("Liquidity buffer is tight", $"The M12 current ratio is {closing.CurrentRatio.GetValueOrDefault():F2}x.", "Build more liquid working capital before committing to non-essential purchases."));
        else
            alerts.Add(Healthy("Sound short-term cover", $"The M12 current ratio is {closing.CurrentRatio.GetValueOrDefault():F2}x.", "Maintain collection discipline and monitor the quality—not only the quantity—of current assets."));

        if (overdraftMonths > 0)
            alerts.Add(Critical("Recurring bank overdraft", $"{overdraftMonths} month(s) project a negative bank position.", "Match payment timing to receipts and secure sufficient working-capital facilities before the first shortfall."));

        if (closing.ClosingLoanBalance > closing.TotalAssets * 0.60m && closing.TotalAssets > 0m)
            alerts.Add(Warning("High loan concentration", $"Closing loans represent {closing.ClosingLoanBalance / closing.TotalAssets * 100m:F1}% of total assets.", "Prioritise principal reduction and avoid funding short-lived assets with long-term debt."));

        if (closing.ClosingDebtors > currentAssetBase * 0.45m)
            alerts.Add(Warning("Debtors dominate current assets", $"Debtors represent {closing.ClosingDebtors / currentAssetBase * 100m:F1}% of current assets.", "Tighten credit approval, collection follow-up and overdue-account escalation."));

        if (closing.ClosingStock > currentAssetBase * 0.50m)
            alerts.Add(Warning("Inventory concentration is high", $"Stock represents {closing.ClosingStock / currentAssetBase * 100m:F1}% of current assets.", "Review ageing and reorder levels to release cash from slow-moving inventory."));

        if (closing.TotalAssets > 0m && closing.ClosingFixedAssets > closing.TotalAssets * 0.80m)
            alerts.Add(Warning("Asset base is relatively illiquid", $"Fixed assets represent {closing.ClosingFixedAssets / closing.TotalAssets * 100m:F1}% of total assets.", "Preserve adequate cash reserves and review underutilised assets before further capital expenditure."));

        return alerts
            .OrderBy(alert => alert.Priority)
            .ThenBy(alert => alert.Title);
    }

    private static BalanceSheetAlert Critical(string title, string finding, string recommendation) =>
        new(0, "critical", "bi-exclamation-octagon-fill", title, finding, recommendation);

    private static BalanceSheetAlert Warning(string title, string finding, string recommendation) =>
        new(1, "warning", "bi-exclamation-triangle-fill", title, finding, recommendation);

    private static BalanceSheetAlert Healthy(string title, string finding, string recommendation) =>
        new(2, "healthy", "bi-check-circle-fill", title, finding, recommendation);

    private void ToggleAlerts() => ShowAllAlerts = !ShowAllAlerts;
    private void DismissAlerts() => ShowAlerts = false;

    private Task PreparePrintAsync()
    {
        if (IsPrinting || IsLoadingAssets || IsLoadingProjection || Projection is null)
            return Task.CompletedTask;

        AlertsExpandedBeforePrint = ShowAllAlerts;
        ShowAllAlerts = true;
        IsPrinting = true;
        PrintRequested = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OpenPrintDialogAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("viqAssetDetailsCharts.print", "balance-sheet");
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Unable to open the balance sheet print dialog");
            ProjectionError = "The balance sheet print preview could not be opened.";
        }
        finally
        {
            ShowAllAlerts = AlertsExpandedBeforePrint;
            IsPrinting = false;
            StateHasChanged();
        }
    }

    private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs args)
    {
        if (args.AssessmentId != AssessmentId)
            return;

        _ = InvokeAsync(async () =>
        {
            await Task.WhenAll(LoadAssetMetricsAsync(), LoadProjectionAsync());
            StateHasChanged();
        });
    }

    public void Dispose() => ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;

    private sealed record BalanceSheetAlert(
        int Priority,
        string Severity,
        string Icon,
        string Title,
        string Finding,
        string Recommendation);
}
