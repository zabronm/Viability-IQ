using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using static ViabilityIQ.Web.Components.CommonComponents.ViqAlertComponent;
using static ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.SalesCategoryFormComponent;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockPage
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }

        private ZabOffCanvas OffCanvasControlRef;
        private string ActivePanelTitle { get; set; } = string.Empty;
        private FormContextType ActiveFormType { get; set; } = FormContextType.Stock;
        private StockManagementViewModel StockInventoryContext { get; set; } = new();
        private decimal BulkStockValueTarget { get; set; }

        private bool blAlert { get; set; } = true;
        private AlertSeverity AlertSeverity { get; set; } = AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Inventory Notice:";
        private string AlertMessage { get; set; } = "Verify that your closing stock values align accurately with your cost of sales allocations for this assessment phase.";

        // Mock/Assumed Context Projections to cross-plot trends elegantly
        private readonly decimal[] MonthlySalesBaselineProjections = new decimal[12]
            { 45000, 48000, 52000, 51000, 58000, 62000, 60000, 65000, 68000, 71000, 74000, 80000 };

        // Basic Operational Evaluation Properties
        private decimal OpeningStockValue => StockInventoryContext?.MonthlyBalances?.FirstOrDefault() ?? 0;
        private decimal ClosingStockValue => StockInventoryContext?.MonthlyBalances?.LastOrDefault() ?? 0;
        private decimal AverageStockValue => StockInventoryContext?.MonthlyBalances?.Average() ?? 0;

        private decimal TotalPurchasesComputed => 245000m;
        private decimal CostOfSalesValue => (OpeningStockValue + TotalPurchasesComputed) - ClosingStockValue;
        private decimal TurnoverTimes => AverageStockValue > 0 ? Math.Round(CostOfSalesValue / AverageStockValue, 2) : 0;
        private decimal StockTurnoverDays => TurnoverTimes > 0 ? Math.Round(365m / TurnoverTimes, 0) : 0;

        // Working Capital Operations Parameters
        private decimal DebtorDaysBaseline => 42m;
        private decimal CreditorDaysBaseline => 30m;
        private decimal CashCycleDays => Math.Round(StockTurnoverDays + DebtorDaysBaseline - CreditorDaysBaseline, 0);

        private string TurnoverRatingText => StockTurnoverDays switch
        {
            <= 30 => "Very Good",
            <= 60 => "Good",
            <= 90 => "Fair",
            _ => "Bad"
        };

        protected override void OnInitialized()
        {
            InitializeStockDefaultSnapshot();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Always initialize or update charts downstream following DOM painting cycles
            await TriggerAnalyticsChartRenderCyclesAsync();
        }

        private void InitializeStockDefaultSnapshot()
        {
            StockInventoryContext = new StockManagementViewModel
            {
                StockRowLabel = "Monthly inventory",
                MonthlyBalances = new decimal[12] { 25000, 27000, 26500, 28000, 31000, 30500, 29000, 33000, 34500, 32000, 35000, 38000 }
            };
            EvaluateStockMetricsAndTriggerAlerts();
        }

        private async Task TriggerAnalyticsChartRenderCyclesAsync()
        {
            try
            {
                var stockArray = StockInventoryContext.MonthlyBalances.Select(m => (double)m).ToArray();
                var salesArray = MonthlySalesBaselineProjections.Select(s => (double)s).ToArray();

                // Invoke JavaScript Interop Handlers to initialize ChartJs Context
                await JSRuntime.InvokeVoidAsync("renderViqSalesStockTrends", "viqSalesStockTrendChart", salesArray, stockArray);
                await JSRuntime.InvokeVoidAsync("renderViqCashCycleBars", "viqCashCycleHorizontalChart", (double)StockTurnoverDays, (double)DebtorDaysBaseline, (double)CreditorDaysBaseline, (double)CashCycleDays);
            }
            catch { /* Intercept pre-rendering abstractions safely */ }
        }

        private void OpenStockDataEntryPanel()
        {
            ActiveFormType = FormContextType.Stock;
            ActivePanelTitle = "Modify Stock Inventory Allocations";
            BulkStockValueTarget = 0;
            OffCanvasControlRef.OpenAsync();
        }

        private void DistributeStockValuesEvenly()
        {
            if (BulkStockValueTarget <= 0) return;
            for (int i = 0; i < 12; i++)
            {
                StockInventoryContext.MonthlyBalances[i] = Math.Round(BulkStockValueTarget, 0);
            }
            BulkStockValueTarget = 0;
            EvaluateStockMetricsAndTriggerAlerts();
        }

        private void EvaluateStockMetricsAndTriggerAlerts()
        {
            if (StockTurnoverDays > 90)
            {
                blAlert = true;
                AlertSeverity = AlertSeverity.Danger;
                AlertHeading = "Critical Turnover Slowdown:";
                AlertMessage = $"Inventory is holding for {StockTurnoverDays} days! This exceeds standard cash flow risk thresholds.";
            }
            else if (StockTurnoverDays <= 30)
            {
                blAlert = true;
                AlertSeverity = AlertSeverity.Success;
                AlertHeading = "Optimized Turnover Efficiency:";
                AlertMessage = "Excellent efficiency setup! Holding days indicate rapid product movement cycles.";
            }
            else
            {
                blAlert = true;
                AlertSeverity = AlertSeverity.Info;
                AlertHeading = "Information Ledger:";
                AlertMessage = "Inventory configurations are active. Adjust parameters using the edit action button framework at any time.";
            }
        }

        private string GetTurnoverRatingBadgeClass() => StockTurnoverDays switch
        {
            <= 30 => "bg-success text-white text-xxs px-1.5 py-0.5 rounded-pill",
            <= 60 => "bg-info text-dark text-xxs px-1.5 py-0.5 rounded-pill",
            <= 90 => "bg-warning text-dark text-xxs px-1.5 py-0.5 rounded-pill",
            _ => "bg-danger text-white text-xxs px-1.5 py-0.5 rounded-pill"
        };

        private void HandleWorkflowStateChanged()
        {
            EvaluateStockMetricsAndTriggerAlerts();
            OffCanvasControlRef.CloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            // Perform clean memory/disposal allocation tracking hooks if required
            await Task.CompletedTask;
        }

        public enum FormContextType { Sales, Sundry, Grants, Expenses, Stock }

        public class StockManagementViewModel
        {
            public string StockRowLabel { get; set; } = "Monthly inventory";
            public decimal[] MonthlyBalances { get; set; } = new decimal[12];
        }


    }
}