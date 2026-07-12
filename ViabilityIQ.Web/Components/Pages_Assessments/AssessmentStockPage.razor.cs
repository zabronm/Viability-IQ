using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockPage : ComponentBase
    {
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }

        [Parameter] public long AssessmentId { get; set; }
       
        private string ActivePanelTitle { get; set; } = string.Empty;        
        private StockManagementViewModel StockInventoryContext { get; set; } = new();

        // ALERT NOTIFICATION VARIABLES
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Inventory Notice:";
        private string AlertMessage { get; set; } = "Verify that your closing stock values align accurately with your cost of sales allocations.";

        private readonly decimal[] MonthlySalesBaselineProjections = new decimal[12]
            { 45000, 48000, 52000, 51000, 58000, 62000, 60000, 65000, 68000, 71000, 74000, 80000 };

        // Basic Evaluation Calculations
        private decimal OpeningStockValue => StockInventoryContext?.MonthlyBalances?.FirstOrDefault() ?? 0;
        private decimal ClosingStockValue => StockInventoryContext?.MonthlyBalances?.LastOrDefault() ?? 0;
        private decimal AverageStockValue => StockInventoryContext?.MonthlyBalances?.Average() ?? 0;

        private decimal TotalPurchasesComputed => 245000m;
        private decimal CostOfSalesValue => (OpeningStockValue + TotalPurchasesComputed) - ClosingStockValue;
        private decimal TurnoverTimes => AverageStockValue > 0 ? Math.Round(CostOfSalesValue / AverageStockValue, 2) : 0;
        private decimal StockTurnoverDays => TurnoverTimes > 0 ? Math.Round(365m / TurnoverTimes, 0) : 0;

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

                await JSRuntime.InvokeVoidAsync("renderViqSalesStockTrends", "viqSalesStockTrendChart", salesArray, stockArray);
                await JSRuntime.InvokeVoidAsync("renderViqCashCycleBars", "viqCashCycleHorizontalChart", (double)StockTurnoverDays, (double)DebtorDaysBaseline, (double)CreditorDaysBaseline, (double)CashCycleDays);
            }
            catch { /* Catch rendering abstraction skips safely */ }
        }


        //================ OPEN THE LOAN REPAYMENTS FORM  ==========================
        private async Task OpenStockDataEntryPanel(long _mode)
        {
            try
            {
                ActivePanelTitle = _mode == 0 ?
                    "Add Stock Balances" : "Edit Stock Balances";

                await zabCanvasService!.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 350,
                        ComponentType = typeof(AssessmentStockFormComponent),
                        Parameters = new
                        {
                            StockContext = StockInventoryContext
                        },

                        ResultCallback = HandleSaveStockChangesAsync                 //Handle results from component
                    });

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {

            }
        }


       

        private async Task HandleSaveStockChangesAsync(SaveResult result)
        {
            if (result.Success)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                EvaluateStockMetricsAndTriggerAlerts();
                StateHasChanged();                

                _Toast.ShowSuccess(result.Message, sessionService!.AppTitle);

                await Task.CompletedTask;
            }

            if (!result.Success)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                _Toast.ShowError(result.Message, sessionService!.AppTitle);
                await Task.CompletedTask;
            }

            if (!result.Cancelled)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                _Toast.ShowInfo("You aborted the operation", sessionService!.AppTitle);
                await Task.CompletedTask;
            }

        }



        private void EvaluateStockMetricsAndTriggerAlerts()
        {
            if (StockTurnoverDays > 90)
            {
                blAlert = true;
                AlertSeverity = ViqAlertComponent.AlertSeverity.Danger;
                AlertHeading = "Critical Turnover Slowdown:";
                AlertMessage = $"Inventory is holding for {StockTurnoverDays} days! This exceeds standard cash flow risk thresholds.";
            }
            else if (StockTurnoverDays <= 30)
            {
                blAlert = true;
                AlertSeverity = ViqAlertComponent.AlertSeverity.Success;
                AlertHeading = "Optimized Turnover Efficiency:";
                AlertMessage = "Excellent efficiency setup! Holding days indicate rapid product movement cycles.";
            }
            else
            {
                blAlert = true;
                AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
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
    }

    public class StockManagementViewModel
    {
        public string StockRowLabel { get; set; } = "Monthly inventory";
        public decimal[] MonthlyBalances { get; set; } = new decimal[12];
    }
}