using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents
{
    public partial class CashflowSummaryDashboardComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies

        [Inject] private ICashflowEngine? CashflowEngine { get; set; }
        [Inject] private IProjectionStateManager? ProjectionStateManager { get; set; }
        [Inject] private ILogger<CashflowSummaryDashboardComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private CashflowSummaryData? SummaryData { get; set; }
        private bool IsLoading = true;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation(
                    "CashflowSummaryDashboardComponent initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadSummaryData();

                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug(
                        "CashflowSummaryDashboardComponent subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing CashflowSummaryDashboardComponent");
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadSummaryData()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug(
                    "Loading cashflow summary for assessment {AssessmentId}",
                    AssessmentId);

                var monthlyCashflows = await CashflowEngine!.GetMonthlyCashflowDisplayAsync(AssessmentId);

                if (monthlyCashflows != null && monthlyCashflows.Count > 0)
                {
                    decimal totalSales = 0;
                    decimal totalCOGS = 0;
                    decimal totalOtherIncome = 0;
                    decimal totalExpense = 0;
                    decimal totalGrossVAT = 0;
                    decimal totalNetVAT = 0;

                    foreach (var monthly in monthlyCashflows)
                    {
                        totalSales += monthly.SalesRevenue;
                        totalCOGS += monthly.COGS;
                        totalOtherIncome += monthly.OtherIncome;
                        totalExpense += monthly.TotalExpense;
                        totalGrossVAT += monthly.GrossVAT;
                        totalNetVAT += monthly.NetVAT;
                    }

                    var grossProfit = totalSales - totalCOGS;
                    var grossIncome = grossProfit + totalOtherIncome;
                    var operatingExpenses = totalExpense - totalCOGS;
                    var ebitda = grossIncome - operatingExpenses;
                    var netProfit = ebitda - totalNetVAT;

                    SummaryData = new CashflowSummaryData
                    {
                        TotalSales = totalSales,
                        CostOfSales = totalCOGS,
                        GrossProfit = grossProfit,
                        GrossProfitMargin = totalSales > 0 ? (grossProfit / totalSales) * 100 : 0,
                        SundryIncome = totalOtherIncome,
                        GrossIncome = grossIncome,
                        OperatingExpenses = operatingExpenses,
                        OpExPercentage = totalSales > 0 ? (operatingExpenses / totalSales) * 100 : 0,
                        EBITDA = ebitda,
                        EBITDAMargin = totalSales > 0 ? (ebitda / totalSales) * 100 : 0,
                        NetProfit = netProfit,
                        NetProfitMargin = totalSales > 0 ? (netProfit / totalSales) * 100 : 0,
                        COGSPercentage = totalSales > 0 ? (totalCOGS / totalSales) * 100 : 0,
                        GrossVAT = totalGrossVAT,
                        NetVAT = totalNetVAT,
                        FinancialHealth = GetFinancialHealth(netProfit, totalSales),
                    };

                    Logger?.LogInformation(
                        "Loaded cashflow summary for assessment {AssessmentId}. " +
                        "Sales: {Sales}, Profit: {Profit}, Margin: {Margin}%",
                        AssessmentId, totalSales, netProfit,
                        (totalSales > 0 ? (netProfit / totalSales) * 100 : 0));
                }
                else
                {
                    Logger?.LogWarning(
                        "No monthly cashflows found for assessment {AssessmentId}",
                        AssessmentId);
                    SummaryData = null;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading cashflow summary for assessment {AssessmentId}", AssessmentId);
                SummaryData = null;
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
                    "Projection changed event received for assessment {AssessmentId}, reloading summary",
                    AssessmentId);

                InvokeAsync(async () =>
                {
                    await LoadSummaryData();
                    StateHasChanged();
                });
            }
        }

        private string GetFinancialHealth(decimal netProfit, decimal sales)
        {
            if (sales <= 0) return "No Data";

            var margin = (netProfit / sales) * 100;

            return margin switch
            {
                >= 20 => "Excellent",
                >= 15 => "Very Good",
                >= 10 => "Good",
                >= 5 => "Fair",
                >= 0 => "Marginal",
                _ => "At Risk"
            };
        }

        private string GetHealthColor(string health)
        {
            return health switch
            {
                "Excellent" => "#16a34a",
                "Very Good" => "#22c55e",
                "Good" => "#84cc16",
                "Fair" => "#eab308",
                "Marginal" => "#f59e0b",
                "At Risk" => "#dc2626",
                _ => "#6b7280"
            };
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
                        "CashflowSummaryDashboardComponent unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing CashflowSummaryDashboardComponent");
            }

            await Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// Cashflow Summary Data Model
    /// </summary>
    public class CashflowSummaryData
    {
        public decimal TotalSales { get; set; }
        public decimal CostOfSales { get; set; }
        public decimal COGSPercentage { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }
        public decimal SundryIncome { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal OpExPercentage { get; set; }
        public decimal EBITDA { get; set; }
        public decimal EBITDAMargin { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal GrossVAT { get; set; }
        public decimal NetVAT { get; set; }
        public string FinancialHealth { get; set; } = "Unknown";
    }
}