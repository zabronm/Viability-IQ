using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents
{
    public partial class DetailedCashflowStatementComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies

        [Inject] private ICashflowEngine? CashflowEngine { get; set; }
        [Inject] private IProjectionStateManager? ProjectionStateManager { get; set; }
        [Inject] private ILogger<DetailedCashflowStatementComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private List<MonthlyCashflowDetail> MonthlyCashflows = new();
        private bool IsLoading = true;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation(
                    "DetailedCashflowStatementComponent initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadDetailedCashflow();

                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug(
                        "DetailedCashflowStatementComponent subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing DetailedCashflowStatementComponent");
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadDetailedCashflow()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug(
                    "Loading detailed cashflow for assessment {AssessmentId}",
                    AssessmentId);

                var monthlyCashflows = await CashflowEngine!.GetMonthlyCashflowDisplayAsync(AssessmentId);

                if (monthlyCashflows != null && monthlyCashflows.Any())
                {
                    MonthlyCashflows = monthlyCashflows
                        .Select((cf, idx) => new MonthlyCashflowDetail
                        {
                            Month = idx + 1,
                            TotalSales = cf.SalesRevenue,
                            CostOfSales = cf.COGS,
                            SundryIncome = cf.OtherIncome,
                            OperatingExpenses = cf.TotalExpense - cf.COGS,
                            GrossVAT = 0, // Will be calculated by ICashflowEngine - add this to engine
                            NetProfit = 0, // Will be calculated by ICashflowEngine - add this to engine
                        })
                        .ToList();

                    Logger?.LogInformation(
                        "Loaded detailed cashflow for assessment {AssessmentId}. " +
                        "Total Sales: {TotalSales}, Total COGS: {COGS}, Total OpEx: {OpEx}",
                        AssessmentId,
                        MonthlyCashflows.Sum(c => c.TotalSales),
                        MonthlyCashflows.Sum(c => c.CostOfSales),
                        MonthlyCashflows.Sum(c => c.OperatingExpenses));
                }
                else
                {
                    Logger?.LogWarning(
                        "No monthly cashflows found for assessment {AssessmentId}",
                        AssessmentId);
                    MonthlyCashflows = new();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading detailed cashflow for assessment {AssessmentId}", AssessmentId);
                MonthlyCashflows = new();
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
                    "Projection changed event received for assessment {AssessmentId}, reloading detailed cashflow",
                    AssessmentId);

                InvokeAsync(async () =>
                {
                    await LoadDetailedCashflow();
                    StateHasChanged();
                });
            }
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
                        "DetailedCashflowStatementComponent unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing DetailedCashflowStatementComponent");
            }

            await Task.CompletedTask;
        }

        #endregion
    }

    
    /// Model for monthly cashflow detail line item
    
    public class MonthlyCashflowDetail
    {
        public int Month { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CostOfSales { get; set; }
        public decimal SundryIncome { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal GrossVAT { get; set; }  // ✅ NEW
        public decimal NetProfit { get; set; }  // ✅ NEW

        public decimal GrossProfit => TotalSales - CostOfSales;
        public decimal GrossIncome => GrossProfit + SundryIncome;
        public decimal EBITDA => GrossIncome - OperatingExpenses;
    }
}