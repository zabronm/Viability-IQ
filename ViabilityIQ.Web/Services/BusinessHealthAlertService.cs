using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Services
{
    
    /// Analyzes cashflow data and generates intelligent business health alerts
    
    public class BusinessHealthAlertService : IBusinessHealthAlertService
    {
        #region Private Fields

        private readonly ICashflowEngine _cashflowEngine;
        private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
        private readonly IGenericDataRepository<AssessmentExpenses> _expenseRepository;
        private readonly ILogger<BusinessHealthAlertService> _logger;

        // Alert threshold constants
        private const decimal CRITICAL_MARGIN = 5m;                 // < 5% operating margin
        private const decimal WARNING_MARGIN = 10m;                 // < 10% operating margin
        private const decimal CRITICAL_EXPENSE_RATIO = 90m;         // > 90% of income
        private const decimal WARNING_EXPENSE_RATIO = 80m;          // > 80% of income
        private const int CRITICAL_NEGATIVE_MONTHS = 3;             // >= 3 months negative
        private const int WARNING_NEGATIVE_MONTHS = 2;              // >= 2 months negative
        private const decimal CRITICAL_BALANCE = 0m;                // Balance goes negative

        #endregion

        #region Constructor

        public BusinessHealthAlertService(
            ICashflowEngine cashflowEngine,
            IGenericDataRepository<AssessmentSales> salesRepository,
            IGenericDataRepository<AssessmentExpenses> expenseRepository,
            ILogger<BusinessHealthAlertService> logger)
        {
            _cashflowEngine = cashflowEngine ?? throw new ArgumentNullException(nameof(cashflowEngine));
            _salesRepository = salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
            _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        
        /// Checks if assessment has any financial data
        
        public async Task<bool> HasDataAsync(long assessmentId)
        {
            try
            {
                var sales = (await _salesRepository.GetAllAsync(s => s.AssessmentId == assessmentId && s.Active)).ToList();
                var expenses = (await _expenseRepository.GetAllAsync(e => e.AssessmentId == assessmentId && e.Active)).ToList();
                var hasData = sales.Any() || expenses.Any();

                _logger.LogDebug("Assessment {AssessmentId} has data: {HasData} (Sales: {SalesCount}, Expenses: {ExpenseCount})",
                    assessmentId, hasData, sales.Count, expenses.Count);

                return hasData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if assessment {AssessmentId} has data", assessmentId);
                return false;
            }
        }

        
        /// Generates all business health alerts for an assessment
        
        public async Task<List<BusinessHealthAlert>> GenerateAlertsAsync(long assessmentId, CashflowSummaryDto summary)
        {
            try
            {
                var alerts = new List<BusinessHealthAlert>();

                // CHECK 1: No data alert (highest priority for new assessments)
                var hasData = await HasDataAsync(assessmentId);
                if (!hasData)
                {
                    alerts.Add(GetOnboardingAlert(assessmentId));
                    _logger.LogInformation("Generated onboarding alert for assessment {AssessmentId} (no data)", assessmentId);
                    return alerts;
                }

                if (summary == null)
                {
                    _logger.LogWarning("Summary is null for assessment {AssessmentId}", assessmentId);
                    return alerts;
                }

                _logger.LogDebug("Generating business health alerts for assessment {AssessmentId}", assessmentId);

                // ============================================================
                // CASH RESERVE ALERTS
                // ============================================================

                // CRITICAL: Negative Cash Balance
                if (summary.MinimumCashBalance < CRITICAL_BALANCE)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Critical,
                        Category = AlertCategory.CashReserve,
                        Title = "🔴 Cash Reserve Critical",
                        Message = $"Projected cash balance goes negative at minimum of {summary.MinimumCashBalance:C2}. " +
                                 "The business cannot meet its obligations. Immediate action required.",
                        Recommendation = "Increase sales, reduce expenses, or arrange financing immediately.",
                        DetailedGuidance = new List<string>
                        {
                            "1. IMMEDIATE ACTIONS:",
                            "   • Contact your bank/lender about emergency financing",
                            "   • Review expense budget for immediate cuts",
                            "   • Accelerate customer collections",
                            "",
                            "2. SHORT-TERM (1-3 months):",
                            "   • Negotiate extended payment terms with suppliers",
                            "   • Launch special promotions to boost sales",
                            "   • Consider seasonal workforce adjustments",
                            "",
                            "3. MEDIUM-TERM (3-6 months):",
                            "   • Restructure fixed costs (rent, salaries)",
                            "   • Evaluate pricing strategy",
                            "   • Explore new revenue streams"
                        },
                        MetricValue = summary.MinimumCashBalance,
                        MetricLabel = "Minimum Balance",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true  // Critical: non-dismissible
                    });
                }

                // WARNING: Low Cash Balance
                else if (summary.MinimumCashBalance < 5000m)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Warning,
                        Category = AlertCategory.CashReserve,
                        Title = "⚠️ Low Cash Reserve",
                        Message = $"Minimum projected cash balance is {summary.MinimumCashBalance:C2}. " +
                                 "This provides limited buffer for unexpected expenses.",
                        Recommendation = "Consider building a larger cash reserve or reducing fixed costs.",
                        DetailedGuidance = new List<string>
                        {
                            "1. BUILD EMERGENCY RESERVE:",
                            "   • Target 3-6 months of operating expenses",
                            "   • Current target: R " + (summary.TotalAnnualExpense / 4).ToString("N0"),
                            "   • Build during high-revenue periods",
                            "",
                            "2. ESTABLISH CREDIT FACILITIES:",
                            "   • Apply for business line of credit NOW (before it's needed)",
                            "   • Typical available: 10-30% of annual revenue",
                            "   • Interest-only if not drawn",
                            "",
                            "3. MONITOR MONTHLY:",
                            "   • Track cash flow weekly during low-season",
                            "   • Prepare contingency plan",
                            "   • Review supplier payment terms"
                        },
                        MetricValue = summary.MinimumCashBalance,
                        MetricLabel = "Minimum Balance",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true  // Warning: dismissible
                    });
                }

                // ============================================================
                // PROFITABILITY ALERTS
                // ============================================================

                // CRITICAL: Very High Expense Ratio
                if (summary.ExpenseRatio > CRITICAL_EXPENSE_RATIO)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Critical,
                        Category = AlertCategory.Profitability,
                        Title = "🔴 Critical Expense Ratio",
                        Message = $"Expenses consume {summary.ExpenseRatio:F1}% of revenue. " +
                                 $"Only {(100m - summary.ExpenseRatio):F1}% remains for profit and growth.",
                        Recommendation = "Negotiate better supplier rates, reduce staff, or cut non-essential expenses immediately.",
                        DetailedGuidance = new List<string>
                        {
                            "1. ANALYZE EXPENSES BY CATEGORY:",
                            "   • COGS (Cost of Goods Sold)",
                            "   • Salaries & wages",
                            "   • Rent & facilities",
                            "   • Utilities & services",
                            "   • Marketing",
                            "",
                            "2. IMMEDIATE COST CUTS (Target: -5-10%):",
                            "   • Renegotiate supplier contracts",
                            "   • Reduce discretionary spending",
                            "   • Eliminate low-margin products/services",
                            "",
                            "3. STRUCTURAL CHANGES (3-6 months):",
                            "   • Review staffing levels",
                            "   • Consider outsourcing non-core functions",
                            "   • Automate repetitive tasks",
                            "   • Relocate to lower-cost facility if possible"
                        },
                        MetricValue = summary.ExpenseRatio,
                        MetricLabel = "Expense Ratio",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // WARNING: Elevated Expense Ratio
                else if (summary.ExpenseRatio > WARNING_EXPENSE_RATIO)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Warning,
                        Category = AlertCategory.Profitability,
                        Title = "⚠️ High Expense Ratio",
                        Message = $"Expenses consume {summary.ExpenseRatio:F1}% of revenue. " +
                                 "This limits profitability and growth capacity.",
                        Recommendation = "Review expense categories and look for cost optimization opportunities.",
                        DetailedGuidance = new List<string>
                        {
                            "1. AUDIT CURRENT EXPENSES:",
                            "   • Break down by fixed vs variable",
                            "   • Identify top 5 expense categories",
                            "   • Calculate per-unit cost",
                            "",
                            "2. BENCHMARK AGAINST INDUSTRY:",
                            "   • Compare ratios to competitors",
                            "   • Industry average likely 50-70%",
                            "   • Target for your business: " + (80m - (summary.ExpenseRatio - 80m)).ToString("F1") + "%",
                            "",
                            "3. OPTIMIZATION PLAN:",
                            "   • Focus on largest expense categories first",
                            "   • Negotiate better rates (volume discounts)",
                            "   • Review & eliminate wastage",
                            "   • Track monthly progress"
                        },
                        MetricValue = summary.ExpenseRatio,
                        MetricLabel = "Expense Ratio",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // CRITICAL: Negative Operating Margin
                if (summary.OperatingMarginRatio < 0)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Critical,
                        Category = AlertCategory.Profitability,
                        Title = "🔴 Negative Operating Margin",
                        Message = $"Operating margin is {summary.OperatingMarginRatio:F1}%. " +
                                 "The business is spending more than it earns. This is unsustainable.",
                        Recommendation = "Increase sales volume, raise prices, or reduce operating costs urgently.",
                        DetailedGuidance = new List<string>
                        {
                            "1. PRICE STRATEGY:",
                            "   • Current revenue: " + summary.TotalAnnualIncome.ToString("C0"),
                            "   • Current expenses: " + summary.TotalAnnualExpense.ToString("C0"),
                            "   • Required price increase: " + ((summary.TotalAnnualExpense / summary.TotalAnnualIncome) - 1).ToString("P1"),
                            "   • Evaluate market tolerance for price increase",
                            "",
                            "2. VOLUME STRATEGY:",
                            "   • Increase sales by: " + ((summary.TotalAnnualExpense / summary.TotalAnnualIncome) - 1).ToString("P1"),
                            "   • Sales channels to expand",
                            "   • Marketing investments needed",
                            "",
                            "3. COST STRATEGY:",
                            "   • Target expense reduction: " + ((1 - (summary.TotalAnnualIncome / summary.TotalAnnualExpense))).ToString("P1"),
                            "   • Staffing review required",
                            "   • Non-essential service cuts",
                            "",
                            "4. STRATEGIC OPTIONS:",
                            "   • Business pivot or repositioning",
                            "   • Consider consulting support",
                            "   • Evaluate viability of business model"
                        },
                        MetricValue = summary.OperatingMarginRatio,
                        MetricLabel = "Operating Margin",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // WARNING: Low Operating Margin
                else if (summary.OperatingMarginRatio < WARNING_MARGIN)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Warning,
                        Category = AlertCategory.Profitability,
                        Title = "⚠️ Low Operating Margin",
                        Message = $"Operating margin is only {summary.OperatingMarginRatio:F1}%. " +
                                 "There's limited room for growth or unexpected costs.",
                        Recommendation = "Focus on efficiency improvements to increase profitability.",
                        DetailedGuidance = new List<string>
                        {
                            "1. MEASURE KEY METRICS:",
                            "   • Cost per unit/customer",
                            "   • Customer acquisition cost",
                            "   • Revenue per employee",
                            "   • Inventory turnover",
                            "",
                            "2. EFFICIENCY IMPROVEMENTS:",
                            "   • Process automation opportunities",
                            "   • Waste reduction initiatives",
                            "   • Technology investments (ROI > 1 year)",
                            "",
                            "3. REVENUE GROWTH:",
                            "   • Focus on high-margin products/services",
                            "   • Increase average transaction value",
                            "   • Improve customer retention",
                            "",
                            "4. TARGET IMPROVEMENT:",
                            "   • Current margin: " + summary.OperatingMarginRatio.ToString("F1") + "%",
                            "   • Target margin: 15-20%",
                            "   • Action plan: 12-month implementation"
                        },
                        MetricValue = summary.OperatingMarginRatio,
                        MetricLabel = "Operating Margin",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // ============================================================
                // SUSTAINABILITY ALERTS
                // ============================================================

                // CRITICAL: Many Negative Months
                if (summary.MonthsWithNegativeCashflow >= CRITICAL_NEGATIVE_MONTHS)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Critical,
                        Category = AlertCategory.Sustainability,
                        Title = "🔴 Seasonal Cashflow Crisis",
                        Message = $"{summary.MonthsWithNegativeCashflow} months projected with negative cashflow. " +
                                 "The business cannot sustain itself during these periods.",
                        Recommendation = "Secure a line of credit, adjust pricing seasonally, or build cash reserves during strong months.",
                        DetailedGuidance = new List<string>
                        {
                            "1. IMMEDIATE: ARRANGE FINANCING",
                            "   • Apply for business line of credit NOW",
                            "   • Amount needed: " + (summary.MinimumCashBalance * -1.2m).ToString("C0") + " (with buffer)",
                            "   • Target rate: Prime + 1-2%",
                            "",
                            "2. SHORT-TERM: SEASONAL ADJUSTMENTS",
                            "   • Implement seasonal pricing (premium in low season)",
                            "   • Launch off-season promotions",
                            "   • Offer prepayments/subscriptions",
                            "   • Negotiate supplier terms (60-90 days vs 30)",
                            "",
                            "3. MEDIUM-TERM: BUILD RESERVES",
                            "   • Calculate total financing gap for off-season",
                            "   • Build reserves during peak season",
                            "   • Target: Cover 2-3 months of low-season",
                            "",
                            "4. LONG-TERM: COUNTER-SEASONAL PRODUCTS",
                            "   • Develop complementary products/services",
                            "   • Expand into counter-cyclical markets",
                            "   • Diversify revenue streams"
                        },
                        MetricValue = summary.MonthsWithNegativeCashflow,
                        MetricLabel = "Negative Months",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // WARNING: Some Negative Months
                else if (summary.MonthsWithNegativeCashflow >= WARNING_NEGATIVE_MONTHS)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Warning,
                        Category = AlertCategory.Sustainability,
                        Title = "⚠️ Seasonal Cashflow Risk",
                        Message = $"{summary.MonthsWithNegativeCashflow} months projected with negative cashflow. " +
                                 "Plan ahead for these periods.",
                        Recommendation = "Build emergency reserves or arrange working capital financing.",
                        DetailedGuidance = new List<string>
                        {
                            "1. IDENTIFY SPECIFIC MONTHS:",
                            "   • Look at cashflow projection month-by-month",
                            "   • Total financing gap needed: " + (summary.MinimumCashBalance * -1).ToString("C0"),
                            "   • Duration of crisis: " + summary.MonthsWithNegativeCashflow + " months",
                            "",
                            "2. BUILD CASH RESERVE:",
                            "   • Save 25-30% of profits in strong months",
                            "   • Target: Cover 2 months of low-season expenses",
                            "   • Monthly savings target: " + (summary.MinimumCashBalance * -0.5m / 12).ToString("C0"),
                            "",
                            "3. SECURE STANDBY CREDIT:",
                            "   • Apply for working capital line",
                            "   • Use only if needed (interest is cheaper than closing business)",
                            "   • Repay in peak season",
                            "",
                            "4. OPERATIONAL ADJUSTMENTS:",
                            "   • Reduce variable costs during low season",
                            "   • Negotiate variable supplier contracts",
                            "   • Plan temporary staffing reductions"
                        },
                        MetricValue = summary.MonthsWithNegativeCashflow,
                        MetricLabel = "Negative Months",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // CRITICAL: Not Sustainable
                if (!summary.IsSustainable)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Critical,
                        Category = AlertCategory.Sustainability,
                        Title = "🔴 Business Not Sustainable",
                        Message = "The business model is not sustainable long-term. " +
                                 "Multiple indicators suggest the business cannot survive without significant changes.",
                        Recommendation = "Conduct a comprehensive business review and create a restructuring plan.",
                        DetailedGuidance = new List<string>
                        {
                            "1. VALIDATE YOUR DATA:",
                            "   • Double-check all sales projections",
                            "   • Verify all expenses are captured",
                            "   • Review any assumptions (inflation, growth rates)",
                            "   • Are numbers realistic and conservative?",
                            "",
                            "2. ANALYZE ROOT CAUSES:",
                            "   • Is it a pricing problem? (prices too low)",
                            "   • Is it a cost problem? (expenses too high)",
                            "   • Is it a volume problem? (sales too low)",
                            "   • Is it a mix problem? (wrong product/customer focus)",
                            "",
                            "3. STRATEGIC OPTIONS:",
                            "   • INCREASE REVENUE: Raise prices, expand market, new products",
                            "   • REDUCE COSTS: Renegotiate contracts, cut non-essentials, relocate",
                            "   • CHANGE FOCUS: Pivot to higher-margin offerings",
                            "   • MERGE/ACQUIRE: Partner with complementary business",
                            "   • EXIT: Close business and pursue alternative",
                            "",
                            "4. GET PROFESSIONAL HELP:",
                            "   • Business consultant (strategic review)",
                            "   • Financial advisor (restructuring options)",
                            "   • Accountant (tax implications of changes)",
                            "   • Industry peer (benchmarking & best practices)"
                        },
                        MetricValue = 0,
                        MetricLabel = "Sustainability Status",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // ============================================================
                // POSITIVE ALERT
                // ============================================================

                // HEALTHY: All indicators green
                if (summary.IsSustainable &&
                    summary.OperatingMarginRatio >= WARNING_MARGIN &&
                    summary.ExpenseRatio < WARNING_EXPENSE_RATIO &&
                    summary.MonthsWithNegativeCashflow == 0)
                {
                    alerts.Add(new BusinessHealthAlert
                    {
                        AlertId = Guid.NewGuid().ToString(),
                        AssessmentId = assessmentId,
                        Severity = AlertSeverityLevel.Healthy,
                        Category = AlertCategory.Sustainability,
                        Title = "✓ Business Health Strong",
                        Message = "All major indicators suggest a healthy, sustainable business. " +
                                 "Cashflow projections are positive throughout the year with good margins.",
                        Recommendation = "Continue current trajectory and focus on growth opportunities.",
                        DetailedGuidance = new List<string>
                        {
                            "1. MAINTAIN CURRENT PERFORMANCE:",
                            "   • Operating Margin: " + summary.OperatingMarginRatio.ToString("F1") + "%",
                            "   • Expense Ratio: " + summary.ExpenseRatio.ToString("F1") + "%",
                            "   • Annual Profit: " + summary.TotalAnnualNetCashflow.ToString("C0"),
                            "",
                            "2. GROWTH OPPORTUNITIES:",
                            "   • Reinvest 30-50% of profits in growth",
                            "   • Expand into new markets/products",
                            "   • Invest in technology/automation",
                            "   • Build team for scaling",
                            "",
                            "3. BUILD STRATEGIC RESERVES:",
                            "   • Emergency fund: 6 months operating expenses",
                            "   • Growth capital: 12 months of revenue expansion costs",
                            "   • Current target: " + (summary.TotalAnnualExpense / 2).ToString("C0"),
                            "",
                            "4. STRATEGIC PLANNING:",
                            "   • 3-year growth plan",
                            "   • Market expansion strategy",
                            "   • Competitive differentiation",
                            "   • Key hire/team development"
                        },
                        MetricValue = summary.OperatingMarginRatio,
                        MetricLabel = "Operating Margin",
                        GeneratedAt = DateTime.UtcNow,
                        IsDismissible = true
                    });
                }

                // Log summary
                _logger.LogInformation(
                    "Generated {AlertCount} business health alerts for assessment {AssessmentId}. " +
                    "Critical: {Critical}, Warning: {Warning}, Healthy: {Healthy}",
                    alerts.Count,
                    assessmentId,
                    alerts.Count(a => a.Severity == AlertSeverityLevel.Critical),
                    alerts.Count(a => a.Severity == AlertSeverityLevel.Warning),
                    alerts.Count(a => a.Severity == AlertSeverityLevel.Healthy));

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating business health alerts for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        
        /// Gets latest alerts (max 3, prioritized by severity)
        
        public async Task<List<BusinessHealthAlert>> GetLatestAlertsAsync(long assessmentId, int maxAlerts = 3)
        {
            try
            {
                _logger.LogDebug("Retrieving latest alerts for assessment {AssessmentId}, maxAlerts: {MaxAlerts}", assessmentId, maxAlerts);

                var summary = await _cashflowEngine.GetCashflowSummaryDisplayAsync(assessmentId);
                var allAlerts = await GenerateAlertsAsync(assessmentId, summary);

                // Sort by severity (Critical first) then by generated time (newest first)
                var prioritizedAlerts = allAlerts
                    .OrderByDescending(a => a.Severity)
                    .ThenByDescending(a => a.GeneratedAt)
                    .Take(maxAlerts)
                    .ToList();

                _logger.LogDebug("Returning {AlertCount} prioritized alerts for assessment {AssessmentId}", prioritizedAlerts.Count, assessmentId);

                return prioritizedAlerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest alerts for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        #endregion

        #region Private Methods

        
        /// Creates the default onboarding alert for new assessments with no data
        
        private BusinessHealthAlert GetOnboardingAlert(long assessmentId)
        {
            return new BusinessHealthAlert
            {
                AlertId = Guid.NewGuid().ToString(),
                AssessmentId = assessmentId,
                Severity = AlertSeverityLevel.Healthy,
                Category = AlertCategory.Sustainability,
                Title = "📋 Getting Started with Cashflow Projections",
                Message = "Welcome! To help you understand your business's financial health, " +
                         "we'll analyze your projected cash flow. Start by entering your sales, " +
                         "expenses, and loan information.",
                Recommendation = "Follow the steps below to build your complete financial projection.",
                DetailedGuidance = new List<string>
                {
                    "STEP 1: ENTER SALES DATA (this page)",
                    "  • Click 'Add Revenue Stream'",
                    "  • Enter product/service name",
                    "  • Enter monthly values (M1-M12) for your 12-month projection",
                    "  • Repeat for each revenue type",
                    "",
                    "STEP 2: ENTER EXPENSES",
                    "  • Go to Expenses section",
                    "  • Add all operating costs:",
                    "    - Cost of Goods Sold (COGS)",
                    "    - Salaries & wages",
                    "    - Rent & facilities",
                    "    - Utilities & services",
                    "    - Marketing & promotion",
                    "    - Other expenses",
                    "",
                    "STEP 3: ENTER STOCK (if applicable)",
                    "  • Go to Stock section",
                    "  • Record inventory values by month",
                    "  • This impacts cashflow timing",
                    "",
                    "STEP 4: ENTER LOANS",
                    "  • Go to Loans section",
                    "  • Add loan repayment schedules",
                    "  • Include interest calculations",
                    "",
                    "STEP 5: REVIEW RESULTS",
                    "  • Check Cashflow Intelligence section",
                    "  • Review KPI cards",
                    "  • Read health alerts",
                    "",
                    "💡 TIPS FOR ACCURATE PROJECTIONS:",
                    "  ✓ Be realistic with sales forecasts (don't over-estimate)",
                    "  ✓ Include ALL fixed and variable expenses",
                    "  ✓ Account for seasonal variations",
                    "  ✓ Use historical data if available",
                    "  ✓ Review and update projections quarterly",
                    "  ✓ Adjust based on market changes"
                },
                MetricValue = 0,
                MetricLabel = "Status",
                GeneratedAt = DateTime.UtcNow,
                IsDismissible = true
            };
        }

        #endregion
    }
}
