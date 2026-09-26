using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentSensitivityPage : ComponentBase, IDisposable
{
    [Inject] private ISensitivityAnalysisService SensitivityService { get; set; } = default!;
    [Inject] private ISensitivityAiAnalysisService SensitivityAiService { get; set; } = default!;
    [Inject] private IGroqSensitivityAiAnalysisService GroqSensitivityAiService { get; set; } = default!;
    [Inject] private ISessionService? SessionService { get; set; }
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentSensitivityPage> Logger { get; set; } = default!;

    [CascadingParameter(Name = "CurrentAssessmentId")] public long? CascadedAssessmentId { get; set; }
    [Parameter] public long RouteAssessmentId { get; set; }

    private long AssessmentId { get; set; }
    private SensitivityTab ActiveTab { get; set; } = SensitivityTab.Builder;
    private bool IsLoading { get; set; }
    private bool IsRunningSweep { get; set; }
    private string? ErrorMessage { get; set; }
    private ScenarioAnalysisResult? BaselineResult { get; set; }
    private ScenarioAnalysisResult? CurrentResult { get; set; }
    private SensitivitySweepResult? SweepResult { get; set; }
    private SensitivityRiskResult? RiskResult { get; set; }
    private SensitivityAiConfiguration AiConfiguration { get; set; } = default!;
    private SensitivityAiAnalysisResult? AiAnalysis { get; set; }
    private bool AiConsentGiven { get; set; }
    private bool IsGeneratingAiAnalysis { get; set; }
    private CancellationTokenSource? _aiCancellation;
    private SensitivityAiConfiguration GroqAiConfiguration { get; set; } = default!;
    private SensitivityAiAnalysisResult? GroqAiAnalysis { get; set; }
    private bool GroqAiConsentGiven { get; set; }
    private bool IsGeneratingGroqAiAnalysis { get; set; }
    private CancellationTokenSource? _groqAiCancellation;
    private long _loadedAssessmentId;

    private SensitivityScenario WorkingScenario { get; set; } = NewCustomScenario();
    private SensitivityDriver SelectedDriver { get; set; } = SensitivityDriver.RevenuePercent;
    private decimal AdjustmentValue { get; set; } = -10m;
    private int EffectiveMonth { get; set; } = 1;
    private int DurationMonths { get; set; } = 12;
    private bool IsOngoing { get; set; } = true;

    private SensitivityDriver SweepDriver { get; set; } = SensitivityDriver.RevenuePercent;
    private decimal SweepMinimum { get; set; } = -30m;
    private decimal SweepMaximum { get; set; } = 20m;
    private decimal SweepStep { get; set; } = 5m;
    private int SweepEffectiveMonth { get; set; } = 1;
    private decimal MinimumCashReserve { get; set; }

    private readonly List<SensitivityScenario> _scenarios = PresetScenarios();
    private readonly HashSet<Guid> _comparisonSelection = [];
    private readonly Dictionary<Guid, ScenarioAnalysisResult> _comparisonResults = [];

    private bool BaselineDraft =>
        SessionService is not null
        && (!SessionService.HasSalesEntries || !SessionService.HasExpensesEntries);

    protected override void OnInitialized()
    {
        AiConfiguration = SensitivityAiService.GetConfiguration();
        GroqAiConfiguration = GroqSensitivityAiService.GetConfiguration();
        if (SessionService is not null)
            SessionService.OnSessionChanged += OnSessionChanged;
        ResolveAssessmentContext();
    }

    protected override async Task OnParametersSetAsync()
    {
        ResolveAssessmentContext();
        if (AssessmentId > 0 && _loadedAssessmentId != AssessmentId)
        {
            _loadedAssessmentId = AssessmentId;
            await LoadBaselineAsync();
        }
    }

    private void ResolveAssessmentContext()
    {
        AssessmentId = RouteAssessmentId > 0
            ? RouteAssessmentId
            : CascadedAssessmentId is > 0
                ? CascadedAssessmentId.Value
                : SessionService?.AssessmentId ?? 0;
    }

    private async Task LoadBaselineAsync()
    {
        ResetAiState(resetConsent: true);
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            BaselineResult = await SensitivityService.CalculateScenarioAsync(
                AssessmentId,
                new SensitivityScenario { Name = "Baseline", Description = "Authoritative projection", IsBaseline = true });
            CurrentResult = BaselineResult;
            RiskResult = await SensitivityService.AnalyseRisksAsync(AssessmentId, MinimumCashReserve);
        }
        catch (Exception exception)
        {
            BaselineResult = null;
            CurrentResult = null;
            ErrorMessage = "Sensitivity analysis could not be loaded. " + exception.Message;
            Logger.LogError(exception, "Unable to load sensitivity analysis for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectTab(SensitivityTab tab) => ActiveTab = tab;

    private void AddAdjustment()
    {
        int? duration = IsOngoing ? null : Math.Clamp(DurationMonths, 1, 12 - EffectiveMonth + 1);
        WorkingScenario.Adjustments.Add(new(
            SelectedDriver,
            AdjustmentValue,
            Math.Clamp(EffectiveMonth, 1, 12),
            duration));
    }

    private void RemoveAdjustment(ScenarioAdjustment adjustment) =>
        WorkingScenario.Adjustments.Remove(adjustment);

    private void ApplyPreset(string name)
    {
        var preset = _scenarios.First(x => x.Name == name);
        WorkingScenario = CopyScenario(preset);
        CurrentResult = BaselineResult;
    }

    private async Task RunScenarioAsync()
    {
        ErrorMessage = null;
        try
        {
            CurrentResult = await SensitivityService.CalculateScenarioAsync(AssessmentId, CopyScenario(WorkingScenario));
            ResetAiState(resetConsent: false);
        }
        catch (Exception exception)
        {
            ErrorMessage = "The scenario calculation failed. " + exception.Message;
            Logger.LogError(exception, "Scenario calculation failed for assessment {AssessmentId}", AssessmentId);
        }
    }

    private async Task RunSweepAsync()
    {
        IsRunningSweep = true;
        ErrorMessage = null;
        try
        {
            SweepResult = await SensitivityService.RunSweepAsync(
                AssessmentId,
                new(SweepDriver, SweepMinimum, SweepMaximum, SweepStep, SweepEffectiveMonth));
        }
        catch (Exception exception)
        {
            SweepResult = null;
            ErrorMessage = "The sensitivity range could not be calculated. " + exception.Message;
            Logger.LogError(exception, "Sensitivity sweep failed for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsRunningSweep = false;
        }
    }

    private async Task ToggleComparisonAsync(SensitivityScenario scenario, bool selected)
    {
        if (selected)
        {
            if (_comparisonSelection.Count >= 3)
                return;
            _comparisonSelection.Add(scenario.Id);
            if (!_comparisonResults.ContainsKey(scenario.Id))
                _comparisonResults[scenario.Id] = await SensitivityService.CalculateScenarioAsync(AssessmentId, scenario);
        }
        else
        {
            _comparisonSelection.Remove(scenario.Id);
        }
    }

    private async Task RefreshRisksAsync()
    {
        try
        {
            RiskResult = await SensitivityService.AnalyseRisksAsync(AssessmentId, MinimumCashReserve);
        }
        catch (Exception exception)
        {
            ErrorMessage = "Risk thresholds could not be calculated. " + exception.Message;
            Logger.LogError(exception, "Risk analysis failed for assessment {AssessmentId}", AssessmentId);
        }
    }

    private async Task GenerateAiAnalysisAsync()
    {
        if (CurrentResult is null || BaselineResult is null || !AiConsentGiven || IsGeneratingAiAnalysis)
            return;

        _aiCancellation?.Cancel();
        _aiCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _aiCancellation = cancellation;
        IsGeneratingAiAnalysis = true;
        AiAnalysis = null;
        try
        {
            var analysis = await SensitivityAiService.AnalyseAsync(
                BuildAiRequest(CurrentResult, BaselineResult),
                AiConsentGiven,
                cancellation.Token);
            if (ReferenceEquals(_aiCancellation, cancellation))
                AiAnalysis = analysis;
        }
        finally
        {
            if (ReferenceEquals(_aiCancellation, cancellation))
                IsGeneratingAiAnalysis = false;
        }
    }

    private void ClearAiAnalysis()
    {
        _aiCancellation?.Cancel();
        _aiCancellation = null;
        AiAnalysis = null;
        IsGeneratingAiAnalysis = false;
    }

    private void ResetAiState(bool resetConsent)
    {
        ClearAiAnalysis();
        ClearGroqAiAnalysis();
        if (resetConsent)
        {
            AiConsentGiven = false;
            GroqAiConsentGiven = false;
        }
    }

    private async Task GenerateGroqAiAnalysisAsync()
    {
        if (CurrentResult is null
            || BaselineResult is null
            || !GroqAiConsentGiven
            || IsGeneratingGroqAiAnalysis)
            return;

        _groqAiCancellation?.Cancel();
        _groqAiCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _groqAiCancellation = cancellation;
        IsGeneratingGroqAiAnalysis = true;
        GroqAiAnalysis = null;
        try
        {
            var analysis = await GroqSensitivityAiService.AnalyseAsync(
                BuildAiRequest(CurrentResult, BaselineResult),
                GroqAiConsentGiven,
                cancellation.Token);
            if (ReferenceEquals(_groqAiCancellation, cancellation))
                GroqAiAnalysis = analysis;
        }
        finally
        {
            if (ReferenceEquals(_groqAiCancellation, cancellation))
                IsGeneratingGroqAiAnalysis = false;
        }
    }

    private void ClearGroqAiAnalysis()
    {
        _groqAiCancellation?.Cancel();
        _groqAiCancellation?.Dispose();
        _groqAiCancellation = null;
        GroqAiAnalysis = null;
        IsGeneratingGroqAiAnalysis = false;
    }

    private static SensitivityAiAnalysisRequest BuildAiRequest(
        ScenarioAnalysisResult current,
        ScenarioAnalysisResult baseline) => new()
        {
            CaseType = current.Scenario.IsBaseline ? "Baseline" : "Adjusted scenario",
            Metrics = ToAiMetrics(current.Metrics),
            BaselineMetrics = ToAiMetrics(baseline.Metrics),
            Months = current.Projection.Months
                .Take(12)
                .Select(month => new SensitivityAiMonthSnapshot(
                    month.MonthNumber,
                    month.Revenue,
                    month.ProfitBeforeTax,
                    month.ClosingBank,
                    month.NetCashflow,
                    month.ClosingDebtors,
                    month.ClosingCreditors))
                .ToArray(),
            Adjustments = current.Scenario.Adjustments
                .Select(adjustment => new SensitivityAiAdjustmentSnapshot(
                    DriverName(adjustment.Driver),
                    adjustment.Value,
                    adjustment.EffectiveStartMonth,
                    adjustment.DurationMonths))
                .ToArray(),
            DeterministicFlags = current.Findings
                .Take(10)
                .Select(finding => $"{finding.Title}: {finding.Detail}")
                .ToArray()
        };

    private static SensitivityAiMetricSnapshot ToAiMetrics(SensitivityMetrics metrics) => new(
        metrics.Revenue,
        metrics.GrossProfit,
        metrics.GrossMarginPercent,
        metrics.EBITDA,
        metrics.ProfitBeforeTax,
        metrics.ClosingCash,
        metrics.MinimumCash,
        metrics.MinimumCashMonth,
        metrics.FundingShortfall,
        metrics.CurrentRatio,
        metrics.InterestCover,
        metrics.BreakEvenSales,
        metrics.MarginOfSafety);

    private IEnumerable<ScenarioAnalysisResult> SelectedComparisons =>
        _scenarios.Where(x => _comparisonSelection.Contains(x.Id))
            .Select(x => _comparisonResults.GetValueOrDefault(x.Id))
            .OfType<ScenarioAnalysisResult>();

    private async Task ExportSweepCsvAsync()
    {
        if (SweepResult is null) return;
        var csv = new StringBuilder("Driver value,Revenue,Gross profit,Gross margin,EBITDA,PBT,Closing cash,Minimum cash,Minimum cash month,Funding shortfall,Current ratio,Interest cover,Status,Flags\r\n");
        foreach (var point in SweepResult.Points)
        {
            var m = point.Metrics;
            csv.AppendLine(string.Join(",",
                Csv(point.DriverValue), Csv(m.Revenue), Csv(m.GrossProfit), Csv(m.GrossMarginPercent),
                Csv(m.EBITDA), Csv(m.ProfitBeforeTax), Csv(m.ClosingCash), Csv(m.MinimumCash),
                m.MinimumCashMonth, Csv(m.FundingShortfall), Csv(m.CurrentRatio), Csv(m.InterestCover),
                point.Status, Quote(string.Join("; ", point.Flags))));
        }
        await DownloadCsvAsync($"sensitivity-results-{AssessmentId}.csv", csv.ToString());
    }

    private async Task ExportComparisonCsvAsync()
    {
        if (BaselineResult is null) return;
        var results = new[] { BaselineResult }.Concat(SelectedComparisons).ToArray();
        var csv = new StringBuilder("Scenario,Month,Revenue,Profit before tax,Closing cash,Revenue variance,PBT variance,Cash variance\r\n");
        foreach (var result in results)
        {
            for (var i = 0; i < result.Projection.Months.Count; i++)
            {
                var month = result.Projection.Months[i];
                var baseline = BaselineResult.Projection.Months[i];
                csv.AppendLine(string.Join(",", Quote(result.Scenario.Name), month.MonthLabel,
                    Csv(month.Revenue), Csv(month.ProfitBeforeTax), Csv(month.ClosingBank),
                    Csv(month.Revenue - baseline.Revenue), Csv(month.ProfitBeforeTax - baseline.ProfitBeforeTax),
                    Csv(month.ClosingBank - baseline.ClosingBank)));
            }
        }
        await DownloadCsvAsync($"scenario-comparison-{AssessmentId}.csv", csv.ToString());
    }

    private Task PrintAsync() =>
        JS.InvokeVoidAsync(
            "viqAssessmentPagePrint.print",
            "#sensitivity-print-report").AsTask();

    private async Task DownloadCsvAsync(string filename, string csv)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
        await JS.InvokeVoidAsync("downloadFile", filename, base64, "text/csv;charset=utf-8");
    }

    private static string Csv(decimal? value) =>
        value?.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static decimal Variance(decimal scenario, decimal baseline) => scenario - baseline;
    private static decimal? VariancePercent(decimal scenario, decimal baseline) =>
        baseline == 0m ? null : (scenario - baseline) / Math.Abs(baseline) * 100m;

    private static string KpiPerformanceClass(decimal value, decimal baseline)
    {
        if (value < 0m)
            return "kpi-critical";
        if (baseline <= 0m)
            return value > 0m ? "kpi-very-good" : "kpi-average";

        var performance = value / baseline;
        if (performance >= 1.1m)
            return "kpi-very-good";
        if (performance >= 0.9m)
            return "kpi-good";
        return "kpi-average";
    }

    private static string GrossMarginKpiClass(decimal? grossMarginPercent) =>
        grossMarginPercent switch
        {
            null => "kpi-good",
            < 0m => "kpi-critical",
            < 20m => "kpi-average",
            < 30m => "kpi-good",
            _ => "kpi-very-good"
        };

    private static string FundingShortfallKpiClass(decimal fundingShortfall) =>
        fundingShortfall > 0m ? "kpi-critical" : "kpi-very-good";

    private static decimal ChartWidth(decimal value, IEnumerable<decimal> values)
    {
        var maximum = values.Select(Math.Abs).DefaultIfEmpty(0m).Max();
        return maximum == 0m ? 0m : Math.Max(2m, Math.Abs(value) / maximum * 100m);
    }

    private static string DriverName(SensitivityDriver driver) =>
        SensitivityAnalysisService.DriverLabel(driver);

    private static string DriverUnit(SensitivityDriver driver) => driver switch
    {
        SensitivityDriver.CostOfSalesPercentagePoints or SensitivityDriver.BadDebtPercentagePoints => "percentage points",
        SensitivityDriver.DebtorCollectionShiftMonths or SensitivityDriver.CreditorPaymentShiftMonths => "months",
        SensitivityDriver.MinimumCashReserve => "amount",
        _ => "%"
    };

    private static SensitivityScenario CopyScenario(SensitivityScenario source) => new()
    {
        Name = source.Name,
        Description = source.Description,
        Adjustments = [.. source.Adjustments]
    };

    private static SensitivityScenario NewCustomScenario() => new()
    {
        Name = "Custom",
        Description = "Unsaved working scenario"
    };

    private static List<SensitivityScenario> PresetScenarios() =>
    [
        Scenario("Optimistic", "Stronger sales with disciplined costs",
            new ScenarioAdjustment(SensitivityDriver.RevenuePercent, 10m),
            new ScenarioAdjustment(SensitivityDriver.OperatingExpensesPercent, 3m)),
        Scenario("Expected", "Central planning case"),
        Scenario("Pessimistic", "Lower sales and higher operating costs",
            new ScenarioAdjustment(SensitivityDriver.RevenuePercent, -15m),
            new ScenarioAdjustment(SensitivityDriver.OperatingExpensesPercent, 10m)),
        Scenario("Sales Decline", "Moderate demand shock",
            new ScenarioAdjustment(SensitivityDriver.RevenuePercent, -20m)),
        Scenario("Cost Inflation", "Input and overhead inflation",
            new ScenarioAdjustment(SensitivityDriver.CostOfSalesRelativePercent, 10m),
            new ScenarioAdjustment(SensitivityDriver.OperatingExpensesPercent, 8m)),
        Scenario("Slow Collections", "Customer receipts delayed by one month",
            new ScenarioAdjustment(SensitivityDriver.DebtorCollectionShiftMonths, 1m)),
        Scenario("Interest Rate Increase", "Finance costs increase",
            new ScenarioAdjustment(SensitivityDriver.LoanInterestPercent, 25m)),
        Scenario("Asset Acquisition", "Additional capital expenditure",
            new ScenarioAdjustment(SensitivityDriver.AssetPurchasesPercent, 50m)),
        Scenario("Combined Downside", "Sales, cost and collection pressure",
            new ScenarioAdjustment(SensitivityDriver.RevenuePercent, -20m),
            new ScenarioAdjustment(SensitivityDriver.CostOfSalesRelativePercent, 10m),
            new ScenarioAdjustment(SensitivityDriver.OperatingExpensesPercent, 10m),
            new ScenarioAdjustment(SensitivityDriver.DebtorCollectionShiftMonths, 1m)),
        Scenario("Growth Opportunity", "Sales growth supported by additional spending",
            new ScenarioAdjustment(SensitivityDriver.RevenuePercent, 20m),
            new ScenarioAdjustment(SensitivityDriver.OperatingExpensesPercent, 8m))
    ];

    private static SensitivityScenario Scenario(
        string name,
        string description,
        params ScenarioAdjustment[] adjustments) => new()
        {
            Name = name,
            Description = description,
            Adjustments = [.. adjustments]
        };

    private void OnSessionChanged()
    {
        _ = InvokeAsync(async () =>
        {
            ResolveAssessmentContext();
            if (AssessmentId > 0 && _loadedAssessmentId != AssessmentId)
            {
                _loadedAssessmentId = AssessmentId;
                await LoadBaselineAsync();
            }
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        _aiCancellation?.Cancel();
        _aiCancellation?.Dispose();
        _aiCancellation = null;
        _groqAiCancellation?.Cancel();
        _groqAiCancellation?.Dispose();
        _groqAiCancellation = null;
        if (SessionService is not null)
            SessionService.OnSessionChanged -= OnSessionChanged;
        GC.SuppressFinalize(this);
    }

    private enum SensitivityTab { Builder, Results, Comparison, Risk, AiAnalysis, GroqAiAnalysis }
}
