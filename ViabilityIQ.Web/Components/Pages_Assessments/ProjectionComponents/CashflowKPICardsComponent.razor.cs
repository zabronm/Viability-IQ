using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents
{
    public partial class CashflowKPICardsComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies

        [Inject] private ICashflowEngine? CashflowEngine { get; set; }
        [Inject] private IProjectionStateManager? ProjectionStateManager { get; set; }
        [Inject] private ILogger<CashflowKPICardsComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private CashflowKPIData? KPIData { get; set; }
        private bool IsLoading = true;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation(
                    "CashflowKPICardsComponent initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadKPIData();

                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug(
                        "CashflowKPICardsComponent subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing CashflowKPICardsComponent");
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadKPIData()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug(
                    "Loading KPI data for assessment {AssessmentId}",
                    AssessmentId);

                // Get summary
                var summary = await CashflowEngine!.GetCashflowSummaryDisplayAsync(AssessmentId);

                // Get monthly cashflows for additional metrics
                var monthlyCashflows = await CashflowEngine!.GetMonthlyCashflowDisplayAsync(AssessmentId);

                if (summary != null && monthlyCashflows != null && monthlyCashflows.Count > 0)
                {
                    var totalSales = summary.TotalAnnualIncome;
                    var totalExpenses = summary.TotalAnnualExpense;
                    var grossProfit = summary.TotalAnnualNetCashflow; // This is actually income - expenses
                    var ebitda = summary.TotalAnnualNetCashflow; // Same for now, adjust based on engine
                    var netProfit = summary.TotalAnnualNetCashflow; // Adjust based on engine

                    KPIData = new CashflowKPIData
                    {
                        TotalSales = totalSales,
                        SalesGrowth = 12.4m, // Calculate or get from engine
                        OpeningStock = monthlyCashflows[0].OpeningBalance,
                        ClosingStock = monthlyCashflows[^1].ClosingBalance,
                        ClosingStockVariance = -8.3m, // Calculate or get from engine
                        TotalExpenses = totalExpenses,
                        VATNet = totalSales * 0.15m, // Adjust VAT calculation
                        VATPct = 15m,
                        GrossProfit = grossProfit,
                        GrossProfitMargin = totalSales > 0 ? (grossProfit / totalSales) * 100 : 0,
                        EBITDA = ebitda,
                        EBITDAMargin = totalSales > 0 ? (ebitda / totalSales) * 100 : 0,
                        NetProfit = netProfit,
                        NetProfitMargin = totalSales > 0 ? (netProfit / totalSales) * 100 : 0,
                    };

                    Logger?.LogInformation(
                        "Loaded KPI data for assessment {AssessmentId}. Sales: {Sales}, NetProfit: {NetProfit}",
                        AssessmentId, KPIData.TotalSales, KPIData.NetProfit);
                }
                else
                {
                    Logger?.LogWarning(
                        "No summary or monthly cashflows found for assessment {AssessmentId}",
                        AssessmentId);
                    KPIData = null;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading KPI data for assessment {AssessmentId}", AssessmentId);
                KPIData = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading KPIs",
                    AssessmentId);

                InvokeAsync(async () =>
                {
                    await LoadKPIData();
                    StateHasChanged();
                });
            }
        }

        private string FormatPercentage(decimal value)
        {
            return value >= 0
                ? $"+{value:F1}%"
                : $"{value:F1}%";
        }

        #endregion

        #region Disposal

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            try
            {
                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged -= OnProjectionChanged;
                    Logger?.LogDebug(
                        "CashflowKPICardsComponent unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing CashflowKPICardsComponent");
            }

            await Task.CompletedTask;
        }

        #endregion
    }

    
    /// KPI Data Model
    
    public class CashflowKPIData
    {
        public decimal TotalSales { get; set; }
        public decimal SalesGrowth { get; set; }

        public decimal OpeningStock { get; set; }

        public decimal ClosingStock { get; set; }
        public decimal ClosingStockVariance { get; set; }

        public decimal TotalExpenses { get; set; }

        public decimal VATNet { get; set; }
        public decimal VATPct { get; set; }

        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }

        public decimal EBITDA { get; set; }
        public decimal EBITDAMargin { get; set; }

        public decimal NetProfit { get; set; }
        public decimal NetProfitMargin { get; set; }
    }
}