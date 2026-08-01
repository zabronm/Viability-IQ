using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;


/// Engine for calculating and managing cashflow projections
/// Aggregates data from existing Sales, Expenses, Stock, and Loan data models
/// 
namespace ViabilityIQ.Application.FinancialCalculations
{
    public class CashflowEngine : ICashflowEngine
    {
        #region Private Fields

        private readonly IGenericDataRepository<Assessment> _assessmentRepository;
        private readonly IGenericDataRepository<AssessmentSales> _salesRepository;
        private readonly IGenericDataRepository<AssessmentExpenses> _expensesRepository;
        private readonly IGenericDataRepository<AssessmentStock> _stockRepository;
        private readonly IGenericDataRepository<AssessmentLoanRepayment> _loanRepaymentRepository;
        private readonly ICashflowRepository _cashflowRepository;
        private readonly IProjectionStateManager _projectionStateManager;
        private readonly ILogger<CashflowEngine> _logger;

        //CRITICAL BALANCE THRESHHOLD - BELOW THIS TRIGGERS WARNINGS
        private const decimal CRITICAL_BALANCE_THRESHOLD = 0m;
        private const decimal WARNING_BALANCE_THRESHOLD = 5000m;

        #endregion

        #region Constructor
        public CashflowEngine(  IGenericDataRepository<Assessment> assessmentRepository,
                                IGenericDataRepository<AssessmentSales> salesRepository,
                                IGenericDataRepository<AssessmentExpenses> expensesRepository,
                                IGenericDataRepository<AssessmentLoanRepayment> loanRepaymentRepository,
                                IGenericDataRepository<AssessmentStock> stockRepository,
                                ICashflowRepository cashflowRepository,
                                IProjectionStateManager projectionStateManager,
                                ILogger<CashflowEngine> logger)
        {
            _assessmentRepository = assessmentRepository ?? throw new ArgumentNullException(nameof(assessmentRepository));
            _salesRepository = salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
            _expensesRepository = expensesRepository ?? throw new ArgumentNullException(nameof(expensesRepository));
            _loanRepaymentRepository = loanRepaymentRepository ?? throw new ArgumentNullException(nameof(loanRepaymentRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _cashflowRepository = cashflowRepository ?? throw new ArgumentNullException(nameof(cashflowRepository));
            _projectionStateManager = projectionStateManager ?? throw new ArgumentNullException(nameof(projectionStateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion


        #region Public Methods      
        /// Calculates cashflow for all 12 months by aggregating existing data



        
        /// Calculates cashflow for all 12 months by aggregating existing data
        
        public async Task<List<AssessmentCashflow>> CalculateMonthlyCashflowAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Starting monthly cashflow calculation for assessment {AssessmentId}", assessmentId);

                var assessment = await _assessmentRepository.GetByIdAsync(assessmentId);
                if (assessment == null)
                {
                    _logger.LogError("Assessment not found: {AssessmentId}", assessmentId);
                    throw new Exception($"Assessment {assessmentId} not found");
                }

                // Get all base transactions with filtering
                var salesList = (await _salesRepository.GetAllAsync(s =>
                    s.AssessmentId == assessmentId && s.Active)).ToList();

                var expensesList = (await _expensesRepository.GetAllAsync(e =>
                    e.AssessmentId == assessmentId && e.Active)).ToList();

                var loanRepayments = (await _loanRepaymentRepository.GetAllAsync(l =>
                    l.AssessmentId == assessmentId && l.Active)).ToList();

                var monthlyData = new List<AssessmentCashflow>();
                decimal runningBalance = assessment.OpeningBalance_Bank;  // ✅ CHANGED

                // Calculate for each month (1-12)
                for (int month = 1; month <= 12; month++)
                {
                    var cashflow = new AssessmentCashflow
                    {
                        AssessmentId = assessmentId,
                        MonthNumber = month,
                        Year = DateTime.UtcNow.Year,
                        OpeningBalance = runningBalance,
                        CreatedAt = DateTime.UtcNow
                    };

                    // ========== INCOME CALCULATION ==========
                    cashflow.SalesRevenue = CalculateMonthTotal(salesList, month);
                    cashflow.OtherIncome = 0;
                    cashflow.TotalIncome = cashflow.SalesRevenue + cashflow.OtherIncome;

                    // ========== EXPENSE CALCULATION ==========
                    cashflow.COGS = CalculateExpenseByType(expensesList, month, "COGS");
                    cashflow.SalaryExpense = CalculateExpenseByType(expensesList, month, "Salary");
                    cashflow.RentExpense = CalculateExpenseByType(expensesList, month, "Rent");
                    cashflow.UtilityExpense = CalculateExpenseByType(expensesList, month, "Utilities");
                    cashflow.MarketingExpense = CalculateExpenseByType(expensesList, month, "Marketing");
                    cashflow.OtherExpense = CalculateExpenseByType(expensesList, month, "Other");
                    cashflow.LoanRepayment = CalculateLoanRepaymentByMonth(loanRepayments, month);

                    cashflow.TotalExpense = cashflow.COGS + cashflow.SalaryExpense + cashflow.RentExpense +
                                           cashflow.UtilityExpense + cashflow.MarketingExpense +
                                           cashflow.LoanRepayment + cashflow.OtherExpense;

                    // ========== CASHFLOW METRICS ==========
                    cashflow.NetCashflow = cashflow.TotalIncome - cashflow.TotalExpense;
                    cashflow.ClosingBalance = runningBalance + cashflow.NetCashflow;
                    cashflow.CumulativeCashflow = cashflow.NetCashflow;

                    cashflow.HasNegativeCashflow = cashflow.NetCashflow < 0;
                    cashflow.IsCritical = cashflow.ClosingBalance < CRITICAL_BALANCE_THRESHOLD;

                    _logger.LogDebug(
                        "Month {Month} cashflow: Income={Income}, Expense={Expense}, Net={Net}, Balance={Balance}",
                        month, cashflow.TotalIncome, cashflow.TotalExpense, cashflow.NetCashflow, cashflow.ClosingBalance);

                    monthlyData.Add(cashflow);
                    runningBalance = cashflow.ClosingBalance;
                }

                // Save to database
                await _cashflowRepository.SaveMonthlyCashflowAsync(monthlyData);

                _logger.LogInformation("Monthly cashflow calculation completed for assessment {AssessmentId}", assessmentId);

                return monthlyData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating monthly cashflow for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }


        /// Calculates cashflow summary for entire year       
        public async Task<CashflowSummary> CalculateCashflowSummaryAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Starting cashflow summary calculation for assessment {AssessmentId}", assessmentId);

                // Get all monthly data
                var monthlyData = await _cashflowRepository.GetMonthlyCashflowAsync(assessmentId);

                if (monthlyData == null || monthlyData.Count == 0)
                {
                    _logger.LogWarning("No monthly cashflow data found, calculating...");
                    monthlyData = await CalculateMonthlyCashflowAsync(assessmentId);
                }

                var summary = new CashflowSummary
                {
                    AssessmentId = assessmentId,
                    CreatedAt = DateTime.UtcNow
                };

                // ========== ANNUAL TOTALS ==========

                summary.TotalAnnualIncome = monthlyData.Sum(m => m.TotalIncome);
                summary.TotalAnnualExpense = monthlyData.Sum(m => m.TotalExpense);
                summary.TotalAnnualNetCashflow = monthlyData.Sum(m => m.NetCashflow);

                // ========== BALANCE METRICS ==========

                summary.MinimumCashBalance = monthlyData.Min(m => m.ClosingBalance);
                summary.MaximumCashBalance = monthlyData.Max(m => m.ClosingBalance);

                // ========== CASHFLOW METRICS ==========

                summary.MonthsWithNegativeCashflow = monthlyData.Count(m => m.NetCashflow < 0);
                summary.AverageMonthlyNetCashflow = monthlyData.Average(m => m.NetCashflow);
                summary.CriticalMonthsCount = monthlyData.Count(m => m.IsCritical);
                summary.CashflowRunway = CalculateCashflowRunway(monthlyData);

                // ========== RATIOS ==========

                summary.OperatingMarginRatio = summary.TotalAnnualIncome > 0
                    ? (summary.TotalAnnualNetCashflow / summary.TotalAnnualIncome) * 100m
                    : 0;

                summary.ExpenseRatio = summary.TotalAnnualIncome > 0
                    ? (summary.TotalAnnualExpense / summary.TotalAnnualIncome) * 100m
                    : 0;

                // ========== HEALTH STATUS ==========

                summary.IsSustainable = DetermineSustainability(summary, monthlyData);
                summary.HealthStatus = DetermineHealthStatus(summary, monthlyData);

                // Save summary
                await _cashflowRepository.SaveCashflowSummaryAsync(summary);

                _logger.LogInformation(
                    "Cashflow summary calculated: Status={Status}, Sustainable={Sustainable}",
                    summary.HealthStatus, summary.IsSustainable);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cashflow summary for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        
        /// Gets monthly cashflow DTOs for display
        
        public async Task<List<CashflowMonthlyDto>> GetMonthlyCashflowDisplayAsync(long assessmentId)
        {
            try
            {
                var monthlyData = await _cashflowRepository.GetMonthlyCashflowAsync(assessmentId);

                if (monthlyData == null || monthlyData.Count == 0)
                {
                    monthlyData = await CalculateMonthlyCashflowAsync(assessmentId);
                }

                return monthlyData.Select(m => new CashflowMonthlyDto
                {
                    MonthNumber = m.MonthNumber,
                    MonthName = GetMonthName(m.MonthNumber),
                    SalesRevenue = m.SalesRevenue,
                    OtherIncome = m.OtherIncome,
                    TotalIncome = m.TotalIncome,
                    COGS = m.COGS,
                    SalaryExpense = m.SalaryExpense,
                    RentExpense = m.RentExpense,
                    UtilityExpense = m.UtilityExpense,
                    MarketingExpense = m.MarketingExpense,
                    LoanRepayment = m.LoanRepayment,
                    OtherExpense = m.OtherExpense,
                    TotalExpense = m.TotalExpense,
                    NetCashflow = m.NetCashflow,
                    OpeningBalance = m.OpeningBalance,
                    ClosingBalance = m.ClosingBalance,
                    CumulativeCashflow = m.CumulativeCashflow,
                    HasNegativeCashflow = m.HasNegativeCashflow,
                    IsCritical = m.IsCritical
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly cashflow display for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        
        /// Gets cashflow summary DTO for display
        
        public async Task<CashflowSummaryDto> GetCashflowSummaryDisplayAsync(long assessmentId)
        {
            try
            {
                var summary = await _cashflowRepository.GetCashflowSummaryAsync(assessmentId);

                if (summary == null)
                {
                    summary = await CalculateCashflowSummaryAsync(assessmentId);
                }

                return new CashflowSummaryDto
                {
                    TotalAnnualIncome = summary.TotalAnnualIncome,
                    TotalAnnualExpense = summary.TotalAnnualExpense,
                    TotalAnnualNetCashflow = summary.TotalAnnualNetCashflow,
                    MinimumCashBalance = summary.MinimumCashBalance,
                    MaximumCashBalance = summary.MaximumCashBalance,
                    AverageMonthlyNetCashflow = summary.AverageMonthlyNetCashflow,
                    MonthsWithNegativeCashflow = summary.MonthsWithNegativeCashflow,
                    CriticalMonthsCount = summary.CriticalMonthsCount,
                    CashflowRunway = summary.CashflowRunway,
                    OperatingMarginRatio = summary.OperatingMarginRatio,
                    ExpenseRatio = summary.ExpenseRatio,
                    HealthStatus = summary.HealthStatus,
                    IsSustainable = summary.IsSustainable
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cashflow summary display for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        
        /// Recalculates cashflow when underlying data changes
        
        public async Task<bool> RecalculateCashflowAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation("Recalculating cashflow for assessment {AssessmentId}", assessmentId);

                // Clear old data
                await _cashflowRepository.ClearCashflowAsync(assessmentId);

                // Recalculate
                await CalculateMonthlyCashflowAsync(assessmentId);
                await CalculateCashflowSummaryAsync(assessmentId);

                // Invalidate projection cache
                await _projectionStateManager.InvalidateDataAsync("cashflow", assessmentId, assessmentId);

                _logger.LogInformation("Cashflow recalculation completed for assessment {AssessmentId}", assessmentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating cashflow for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        
        /// Calculates total for a specific month from sales data
        
        private decimal CalculateMonthTotal(IEnumerable<AssessmentSales> sales, int month)
        {
            if (sales == null || sales.Count() == 0)
                return 0;

            return sales.Sum(s => GetMonthValue(s.MonthlyValues, month));
        }

        
        /// Calculates expense total for a specific month and type
        
        private decimal CalculateExpenseByType(IEnumerable<AssessmentExpenses> expenses, int month, string expenseType)
        {
            if (expenses == null || expenses.Count() == 0)
                return 0;

            // Filter by expense type (you may need to adjust based on your ExpenseType data model)
            var filtered = expenses.Where(e => e.Description != null &&
                                               e.Description.Contains(expenseType, StringComparison.OrdinalIgnoreCase));

            return filtered.Sum(e => GetMonthValue(e.MonthlyValues, month));
        }

        
        /// Calculates loan repayment for a specific month (Expected Repayment + Interest)
        
        private decimal CalculateLoanRepaymentByMonth(IEnumerable<AssessmentLoanRepayment> loanRepayments, int month)
        {
            if (loanRepayments == null || loanRepayments.Count() == 0)
                return 0;

            decimal total = 0;

            // MetricTypeId: 1 = Expected Repayment, 2 = Interest, 3 = Extra
            // For cashflow, we want Expected Repayment (1) + Interest (2)
            var principalAndInterest = loanRepayments.Where(l => l.MetricTypeId == 1 || l.MetricTypeId == 2);

            foreach (var repayment in principalAndInterest)
            {
                total += GetMonthValue(repayment.MonthlyValues, month);
            }

            return total;
        }

        
        /// Gets value for a specific month from the monthly values array
        
        private decimal GetMonthValue(decimal[] monthlyValues, int month)
        {
            if (monthlyValues == null || monthlyValues.Length < month)
                return 0;

            return monthlyValues[month - 1]; // Month is 1-based, array is 0-based
        }

        
        /// Calculates cashflow runway (months until cash depleted)
        
        private decimal CalculateCashflowRunway(List<AssessmentCashflow> monthlyData)
        {
            if (monthlyData.All(m => m.NetCashflow >= 0))
                return 12;

            var criticalMonth = monthlyData.FirstOrDefault(m => m.IsCritical);
            return criticalMonth?.MonthNumber ?? 12;
        }

        
        /// Determines if business is sustainable
        
        private bool DetermineSustainability(CashflowSummary summary, List<AssessmentCashflow> monthlyData)
        {
            if (summary.CriticalMonthsCount > 3) return false;
            if (summary.TotalAnnualNetCashflow < 0) return false;
            if (monthlyData.Where(m => m.IsCritical).Count() > 2) return false;

            return true;
        }

        
        /// Determines cashflow health status
        
        private CashflowHealthStatus DetermineHealthStatus(CashflowSummary summary, List<AssessmentCashflow> monthlyData)
        {
            if (summary.CriticalMonthsCount > 2 || summary.MinimumCashBalance < CRITICAL_BALANCE_THRESHOLD)
                return CashflowHealthStatus.Critical;

            if (summary.MonthsWithNegativeCashflow > 2 ||
                summary.MinimumCashBalance < WARNING_BALANCE_THRESHOLD ||
                summary.OperatingMarginRatio < 10)
                return CashflowHealthStatus.Warning;

            return CashflowHealthStatus.Healthy;
        }

        
        /// Gets month name from month number
        
        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January",
                2 => "February",
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => "Unknown"
            };
        }

        #endregion
    }
}

