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
    public partial class FinancialViabilityKPIComponent : ComponentBase, IAsyncDisposable
    {
        #region Injected Dependencies

        [Inject] private ICashflowEngine? CashflowEngine { get; set; }
        [Inject] private IProjectionStateManager? ProjectionStateManager { get; set; }
        [Inject] private ILogger<FinancialViabilityKPIComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private FinancialViabilityData? ViabilityData { get; set; }
        private bool IsLoading = true;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation(
                    "FinancialViabilityKPIComponent initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadViabilityData();

                if (ProjectionStateManager != null)
                {
                    ProjectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug(
                        "FinancialViabilityKPIComponent subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing FinancialViabilityKPIComponent");
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadViabilityData()
        {
            try
            {
                IsLoading = true;

                Logger?.LogDebug(
                    "Loading financial viability data for assessment {AssessmentId}",
                    AssessmentId);

                var monthlyCashflows = await CashflowEngine!.GetMonthlyCashflowDisplayAsync(AssessmentId);
                var summary = await CashflowEngine!.GetCashflowSummaryDisplayAsync(AssessmentId);

                if (monthlyCashflows != null && monthlyCashflows.Count > 0 && summary != null)
                {
                    // Calculate total values
                    decimal totalSales = monthlyCashflows.Sum(m => m.SalesRevenue);
                    decimal totalCOGS = monthlyCashflows.Sum(m => m.COGS);
                    decimal totalExpense = monthlyCashflows.Sum(m => m.TotalExpense);
                    decimal totalProfit = totalSales - totalExpense;

                    // Fixed costs (estimate as ~60% of operating expenses excluding COGS)
                    decimal totalOpEx = totalExpense - totalCOGS;
                    decimal fixedCosts = totalOpEx * 0.6m;

                    // Variable costs
                    decimal variableCosts = totalCOGS + (totalOpEx * 0.4m);

                    // Contribution margin
                    decimal contributionMargin = totalSales - variableCosts;
                    decimal contributionMarginRatio = totalSales > 0 ? (contributionMargin / totalSales) * 100 : 0;

                    // Break-even sales
                    decimal breakEvenSales = contributionMarginRatio > 0
                        ? (fixedCosts / (contributionMarginRatio / 100))
                        : 0;

                    // Margin of safety
                    decimal marginOfSafety = totalSales > 0
                        ? ((totalSales - breakEvenSales) / totalSales) * 100
                        : 0;

                    // Worst case profit (sales reduce by sensitivity range)
                    decimal sensitivityRange = 15m; // 15% variance
                    decimal worstCaseSales = totalSales * (1 - (sensitivityRange / 100));
                    decimal worstCaseProfit = worstCaseSales - fixedCosts - (worstCaseSales * (variableCosts / totalSales));

                    // Current ratio (simplified - assets/liabilities)
                    decimal currentAssets = monthlyCashflows[^1].ClosingBalance;
                    decimal currentLiabilities = totalExpense / 12; // Monthly expenses as proxy
                    decimal currentRatio = currentLiabilities > 0 ? currentAssets / currentLiabilities : 0;

                    // Interest cover (EBITDA / Interest expense - estimated)
                    decimal estimatedInterest = totalSales * 0.02m; // 2% estimated interest
                    decimal ebitda = summary.TotalAnnualNetCashflow;
                    decimal interestCover = estimatedInterest > 0 ? ebitda / estimatedInterest : 0;

                    // NPV calculation (simplified with 10% discount rate)
                    decimal npv = CalculateNPVFromList(monthlyCashflows, 0.10m);

                    // Payback period
                    decimal paybackPeriod = CalculatePaybackPeriodFromList(monthlyCashflows);

                    // Operating leverage
                    decimal operatingLeverage = contributionMarginRatio > 0
                        ? (contributionMargin / totalProfit)
                        : 0;

                    // Debt service coverage
                    decimal debtServiceCoverage = estimatedInterest > 0
                        ? (ebitda / estimatedInterest)
                        : 0;

                    // Profit volatility (standard deviation)
                    var profits = monthlyCashflows.Select(m => m.SalesRevenue - m.TotalExpense).ToList();
                    decimal avgProfit = profits.Average();
                    decimal profitVolatility = profits.Count > 1
                        ? (decimal)Math.Sqrt((double)profits.Sum(p => (p - avgProfit) * (p - avgProfit)) / (profits.Count - 1))
                        : 0;
                    profitVolatility = avgProfit != 0 ? (profitVolatility / avgProfit) * 100 : 0;

                    // Calculate viability score (0-100)
                    decimal viabilityScore = CalculateViabilityScore(
                        marginOfSafety, npv, worstCaseProfit, currentRatio,
                        interestCover, profitVolatility, totalProfit);

                    ViabilityData = new FinancialViabilityData
                    {
                        BreakEvenSales = breakEvenSales,
                        MarginOfSafety = marginOfSafety,
                        NPV = npv,
                        SensitivityRange = sensitivityRange,
                        WorstCaseProfit = worstCaseProfit,
                        CurrentRatio = currentRatio,
                        InterestCover = interestCover,
                        ViabilityScore = viabilityScore,
                        FixedCosts = fixedCosts,
                        ContributionMarginRatio = contributionMarginRatio,
                        PaybackPeriod = paybackPeriod,
                        OperatingLeverage = operatingLeverage,
                        DebtServiceCoverage = debtServiceCoverage,
                        ProfitVolatility = profitVolatility,
                    };

                    Logger?.LogInformation(
                        "Loaded viability data for assessment {AssessmentId}. Score: {Score}, MarginOfSafety: {Margin}%",
                        AssessmentId, viabilityScore, marginOfSafety);
                }
                else
                {
                    Logger?.LogWarning(
                        "No monthly cashflows or summary found for assessment {AssessmentId}",
                        AssessmentId);
                    ViabilityData = null;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading financial viability data for assessment {AssessmentId}", AssessmentId);
                ViabilityData = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Calculate NPV with 10% discount rate
        /// </summary>
        private decimal CalculateNPVFromList(List<CashflowMonthlyDto> cashflows, decimal discountRate)
        {
            decimal npv = 0;
            decimal monthlyRate = discountRate / 12;

            for (int i = 0; i < cashflows.Count; i++)
            {
                decimal monthlyProfit = cashflows[i].SalesRevenue - cashflows[i].TotalExpense;
                decimal discountFactor = (decimal)Math.Pow((double)(1 + monthlyRate), i + 1);
                npv += monthlyProfit / discountFactor;
            }

            return npv;
        }

        /// <summary>
        /// Calculate payback period in months
        /// </summary>
        private decimal CalculatePaybackPeriodFromList(List<CashflowMonthlyDto> cashflows)
        {
            decimal cumulativeProfit = 0;

            for (int i = 0; i < cashflows.Count; i++)
            {
                decimal monthlyProfit = cashflows[i].SalesRevenue - cashflows[i].TotalExpense;
                decimal previousCumulative = cumulativeProfit;
                cumulativeProfit += monthlyProfit;

                if (cumulativeProfit >= 0 && previousCumulative < 0)
                {
                    // Payback occurred in this month
                    return i + (previousCumulative / (previousCumulative - cumulativeProfit));
                }
            }

            return cashflows.Count; // Full period if not paid back
        }

        /// <summary>
        /// Calculate overall viability score (0-100)
        /// </summary>
        private decimal CalculateViabilityScore(
            decimal marginOfSafety, decimal npv, decimal worstCaseProfit,
            decimal currentRatio, decimal interestCover, decimal profitVolatility,
            decimal totalProfit)
        {
            decimal score = 0;

            // Margin of Safety (25 points)
            // Higher margin = better
            score += Math.Min(25, marginOfSafety / 4);

            // NPV (20 points)
            // Positive NPV = 20 points, otherwise less
            score += Math.Max(0, Math.Min(20, (npv > 0 ? 20 : 10)));

            // Worst Case Profit (15 points)
            // Profitable in worst case = 15 points
            score += worstCaseProfit > 0 ? 15 : Math.Max(0, 15 * (1 + (worstCaseProfit / totalProfit)));

            // Current Ratio (15 points)
            // Higher ratio = better liquidity
            score += currentRatio >= 1.5m ? 15 : (currentRatio >= 1m ? 10 : Math.Max(0, 5 * currentRatio));

            // Interest Cover (15 points)
            // Higher cover = better debt service ability
            score += interestCover >= 2m ? 15 : (interestCover >= 1m ? 10 : Math.Max(0, 5 * interestCover));

            // Profit Stability (10 points)
            // Lower volatility = more stable
            score += profitVolatility <= 15 ? 10 : Math.Max(0, 10 - (profitVolatility / 5));

            return Math.Min(100, Math.Max(0, score));
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading viability",
                    AssessmentId);

                InvokeAsync(async () =>
                {
                    await LoadViabilityData();
                    StateHasChanged();
                });
            }
        }

        /// <summary>
        /// Get margin of safety status text
        /// </summary>
        private string GetMarginOfSafetyStatus(decimal margin)
        {
            return margin switch
            {
                >= 50 => "Excellent",
                >= 30 => "Very Good",
                >= 20 => "Good",
                >= 10 => "Fair",
                >= 0 => "Low",
                _ => "At Risk"
            };
        }

        /// <summary>
        /// Get color code for margin of safety
        /// </summary>
        private string GetMargOfSafetyColor(decimal margin)
        {
            return margin switch
            {
                >= 50 => "#16a34a",
                >= 30 => "#22c55e",
                >= 20 => "#84cc16",
                >= 10 => "#eab308",
                >= 0 => "#f59e0b",
                _ => "#dc2626"
            };
        }

        /// <summary>
        /// Get color code for current ratio
        /// </summary>
        private string GetCurrentRatioColor(decimal ratio)
        {
            return ratio switch
            {
                >= 2m => "#16a34a",
                >= 1.5m => "#22c55e",
                >= 1m => "#eab308",
                >= 0.5m => "#f59e0b",
                _ => "#dc2626"
            };
        }

        /// <summary>
        /// Get current ratio status text
        /// </summary>
        private string GetCurrentRatioStatus(decimal ratio)
        {
            return ratio switch
            {
                >= 2m => "Excellent",
                >= 1.5m => "Good",
                >= 1m => "Acceptable",
                >= 0.5m => "Concerning",
                _ => "Critical"
            };
        }

        /// <summary>
        /// Get color code for viability score
        /// </summary>
        private string GetViabilityScoreColor(decimal score)
        {
            return score switch
            {
                >= 80 => "#16a34a",
                >= 70 => "#84cc16",
                >= 60 => "#eab308",
                >= 50 => "#f59e0b",
                >= 40 => "#ff6b6b",
                _ => "#dc2626"
            };
        }

        /// <summary>
        /// Get viability score rating text
        /// </summary>
        private string GetViabilityScoreRating(decimal score)
        {
            return score switch
            {
                >= 80 => "Highly Viable",
                >= 70 => "Viable",
                >= 60 => "Moderately Viable",
                >= 50 => "Marginally Viable",
                >= 40 => "At Risk",
                _ => "High Risk"
            };
        }

        /// <summary>
        /// Generate viability assessment narrative
        /// </summary>
        private string GetViabilityAssessment(FinancialViabilityData data)
        {
            var factors = new List<string>();

            if (data.MarginOfSafety >= 30)
                factors.Add("strong margin of safety");

            if (data.NPV > 0)
                factors.Add("positive net present value");

            if (data.WorstCaseProfit > 0)
                factors.Add("profitable even in worst case");

            if (data.CurrentRatio >= 1.5m)
                factors.Add("adequate liquidity");

            if (data.InterestCover >= 2)
                factors.Add("strong interest coverage");

            if (factors.Any())
            {
                return $"This business demonstrates {string.Join(", ", factors)}. Financial health appears sustainable.";
            }
            else
            {
                return "Risk factors identified. Monitor key metrics closely and consider contingency planning.";
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
                        "FinancialViabilityKPIComponent unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing FinancialViabilityKPIComponent");
            }

            await Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// Financial Viability Data Model
    /// Contains all calculated metrics for financial viability assessment
    /// </summary>
    public class FinancialViabilityData
    {
        /// <summary>
        /// Sales level at which profit equals zero
        /// </summary>
        public decimal BreakEvenSales { get; set; }

        /// <summary>
        /// Percentage by which sales can decline before reaching break-even
        /// </summary>
        public decimal MarginOfSafety { get; set; }

        /// <summary>
        /// Net Present Value of cash flows at 10% discount rate
        /// </summary>
        public decimal NPV { get; set; }

        /// <summary>
        /// Percentage range used for sensitivity analysis
        /// </summary>
        public decimal SensitivityRange { get; set; }

        /// <summary>
        /// Profit in worst case scenario (sales reduced by sensitivity range)
        /// </summary>
        public decimal WorstCaseProfit { get; set; }

        /// <summary>
        /// Ratio of current assets to current liabilities
        /// Measures short-term liquidity
        /// </summary>
        public decimal CurrentRatio { get; set; }

        /// <summary>
        /// Times interest can be covered by EBITDA
        /// Measures ability to service debt
        /// </summary>
        public decimal InterestCover { get; set; }

        /// <summary>
        /// Overall viability score (0-100)
        /// Composite measure of financial health
        /// </summary>
        public decimal ViabilityScore { get; set; }

        /// <summary>
        /// Annual fixed operating costs
        /// </summary>
        public decimal FixedCosts { get; set; }

        /// <summary>
        /// Contribution margin as percentage of sales
        /// </summary>
        public decimal ContributionMarginRatio { get; set; }

        /// <summary>
        /// Number of months to recover initial investment
        /// </summary>
        public decimal PaybackPeriod { get; set; }

        /// <summary>
        /// Degree to which EBIT changes with sales changes
        /// </summary>
        public decimal OperatingLeverage { get; set; }

        /// <summary>
        /// Ratio of EBITDA to debt service obligations
        /// </summary>
        public decimal DebtServiceCoverage { get; set; }

        /// <summary>
        /// Standard deviation of monthly profits as percentage of average
        /// Measures profit stability
        /// </summary>
        public decimal ProfitVolatility { get; set; }
    }
}