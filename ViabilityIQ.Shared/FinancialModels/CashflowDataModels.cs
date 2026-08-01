using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Shared.FinancialModels
{
    [Table("AssessmentCashflow")]
    public class AssessmentCashflow
    {

        [Key] public long CashflowId { get; set; }
        [Required] public long AssessmentId { get; set; }
        [ForeignKey("AssessmentId")] public virtual Assessment Assessment { get; set; }


        /// Month number (1-12)       
        [Required][Range(1, 12)] public int MonthNumber { get; set; }


        /// Year of the projection       
        [Required] public int Year { get; set; }

        // ========== INCOME COMPONENTS ==========


        /// Total sales revenue for the month       
        [Column(TypeName = "decimal(18,2)")] public decimal SalesRevenue { get; set; } = 0;


        /// Other income (grants, investments, etc.)       
        [Column(TypeName = "decimal(18,2)")] public decimal OtherIncome { get; set; } = 0;


        /// Total income for the month       
        [Column(TypeName = "decimal(18,2)")] public decimal TotalIncome { get; set; } = 0;

        // ========== EXPENSE COMPONENTS ==========


        /// Cost of goods sold       
        [Column(TypeName = "decimal(18,2)")] public decimal COGS { get; set; } = 0;


        /// Salary and wage expenses       
        [Column(TypeName = "decimal(18,2)")] public decimal SalaryExpense { get; set; } = 0;


        /// Rent expense       
        [Column(TypeName = "decimal(18,2)")] public decimal RentExpense { get; set; } = 0;


        /// Utility bills (electricity, water, etc.)       
        [Column(TypeName = "decimal(18,2)")] public decimal UtilityExpense { get; set; } = 0;


        /// Marketing and advertising       
        [Column(TypeName = "decimal(18,2)")] public decimal MarketingExpense { get; set; } = 0;


        /// Loan repayment (principal + interest)       
        [Column(TypeName = "decimal(18,2)")] public decimal LoanRepayment { get; set; } = 0;


        /// Other operating expenses       
        [Column(TypeName = "decimal(18,2)")] public decimal OtherExpense { get; set; } = 0;


        /// Total expenses for the month       
        [Column(TypeName = "decimal(18,2)")] public decimal TotalExpense { get; set; } = 0;

        // ========== CASHFLOW METRICS ==========


        /// Net cashflow (Income - Expenses)       
        [Column(TypeName = "decimal(18,2)")] public decimal NetCashflow { get; set; } = 0;


        /// Opening cash balance       
        [Column(TypeName = "decimal(18,2)")] public decimal OpeningBalance { get; set; } = 0;


        /// Closing cash balance       
        [Column(TypeName = "decimal(18,2)")] public decimal ClosingBalance { get; set; } = 0;


        /// Cumulative cashflow (for tracking total cash generated/spent)       
        [Column(TypeName = "decimal(18,2)")] public decimal CumulativeCashflow { get; set; } = 0;


        /// Flag to indicate if this month has negative cashflow (warning indicator)       
        public bool HasNegativeCashflow { get; set; } = false;


        /// Flag to indicate this is a critical month (cash reserve below threshold)       
        public bool IsCritical { get; set; } = false;

        // ========== METADATA ==========

        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string Notes { get; set; } = string.Empty;
    }


    /// Represents detailed cashflow summary for entire projection period

    [Table("CashflowSummary")]
    public class CashflowSummary
    {
        [Key] public long SummaryId { get; set; }

        [Required] public long AssessmentId { get; set; }

        [ForeignKey("AssessmentId")] public virtual Assessment Assessment { get; set; }

        // ========== ANNUAL TOTALS ==========

        [Column(TypeName = "decimal(18,2)")] public decimal TotalAnnualIncome { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")] public decimal TotalAnnualExpense { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")] public decimal TotalAnnualNetCashflow { get; set; } = 0;

        // ========== CASHFLOW HEALTH METRICS ==========


        /// Lowest cash balance during the year

        [Column(TypeName = "decimal(18,2)")] public decimal MinimumCashBalance { get; set; } = 0;


        /// Highest cash balance during the year
        [Column(TypeName = "decimal(18,2)")] public decimal MaximumCashBalance { get; set; } = 0;


        /// Average monthly cashflow
        [Column(TypeName = "decimal(18,2)")] public decimal AverageMonthlyNetCashflow { get; set; } = 0;


        /// Number of months with negative cashflow       
        public int MonthsWithNegativeCashflow { get; set; } = 0;


        /// Number of critical months (cash reserve below threshold)       
        public int CriticalMonthsCount { get; set; } = 0;


        /// Cashflow runway (months until cash runs out, if negative trend)       
        public decimal CashflowRunway { get; set; } = 0;

        // ========== RATIOS & INDICATORS ==========


        /// Operating margin ratio (NetCashflow / TotalIncome)       
        [Column(TypeName = "decimal(5,2)")] public decimal OperatingMarginRatio { get; set; } = 0;


        /// Expense ratio (TotalExpense / TotalIncome)

        [Column(TypeName = "decimal(5,2)")] public decimal ExpenseRatio { get; set; } = 0;


        /// Cashflow health status (Healthy, Warning, Critical)       
        public CashflowHealthStatus HealthStatus { get; set; } = CashflowHealthStatus.Healthy;


        /// Indicates if the business can sustain itself       
        public bool IsSustainable { get; set; } = true;

        // ========== METADATA ==========

        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }


    /// Enumeration for cashflow health status

    public enum CashflowHealthStatus
    {
        Healthy = 0,        // Green: positive cashflow, no concerns
        Warning = 1,        // Yellow: low cashflow or occasional deficits
        Critical = 2        // Red: significant negative cashflow or cash depletion risk
    }


    /// DTO for monthly cashflow display

    public class CashflowMonthlyDto
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; }
        public decimal SalesRevenue { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal TotalIncome { get; set; }

        public decimal COGS { get; set; }
        public decimal SalaryExpense { get; set; }
        public decimal RentExpense { get; set; }
        public decimal UtilityExpense { get; set; }
        public decimal MarketingExpense { get; set; }
        public decimal LoanRepayment { get; set; }
        public decimal OtherExpense { get; set; }
        public decimal TotalExpense { get; set; }

        public decimal NetCashflow { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal CumulativeCashflow { get; set; }

        public bool HasNegativeCashflow { get; set; }
        public bool IsCritical { get; set; }
    }


    /// DTO for cashflow summary display

    public class CashflowSummaryDto
    {
        public decimal TotalAnnualIncome { get; set; }
        public decimal TotalAnnualExpense { get; set; }
        public decimal TotalAnnualNetCashflow { get; set; }

        public decimal MinimumCashBalance { get; set; }
        public decimal MaximumCashBalance { get; set; }
        public decimal AverageMonthlyNetCashflow { get; set; }

        public int MonthsWithNegativeCashflow { get; set; }
        public int CriticalMonthsCount { get; set; }
        public decimal CashflowRunway { get; set; }

        public decimal OperatingMarginRatio { get; set; }
        public decimal ExpenseRatio { get; set; }

        public CashflowHealthStatus HealthStatus { get; set; }
        public bool IsSustainable { get; set; }

        public string HealthStatusText => HealthStatus switch
        {
            CashflowHealthStatus.Healthy => "Healthy",
            CashflowHealthStatus.Warning => "Warning",
            CashflowHealthStatus.Critical => "Critical",
            _ => "Unknown"
        };

        public string SustainabilityText => IsSustainable ? "Sustainable" : "Not Sustainable";
    }
}
