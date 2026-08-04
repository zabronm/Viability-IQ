using Dapper.Contrib.Extensions;
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
    [Dapper.Contrib.Extensions.Table("tblAssessmentCashflow")]
    public class AssessmentCashflow
    {

        [Dapper.Contrib.Extensions.Key] public long AssessmentCashflowId { get; set; }
        [Required] public long AssessmentId { get; set; }

        [Dapper.Contrib.Extensions.Computed]
        [ForeignKey("AssessmentId")] public virtual Assessment Assessment { get; set; }

        /// Month number (1-12)       
        [Required][Range(1, 12)] public int MonthNumber { get; set; }
        [Required] public int Year { get; set; }

        // ========== INCOME COMPONENTS ==========


           
        [Column(TypeName = "decimal(18,2)")] public decimal SalesRevenue { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal OtherIncome { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal TotalIncome { get; set; } = 0;

        // ========== EXPENSE COMPONENTS ==========


        /// Cost of goods sold       
        [Column(TypeName = "decimal(18,2)")] public decimal COGS { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal SalaryExpense { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal RentExpense { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal UtilityExpense { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal MarketingExpense { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal LoanRepayment { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal OtherExpense { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal TotalExpense { get; set; } = 0;

        // ========== CASHFLOW METRICS ==========


       
        [Column(TypeName = "decimal(18,2)")] public decimal NetCashflow { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal OpeningBalance { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal ClosingBalance { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal CumulativeCashflow { get; set; } = 0;
        public bool HasNegativeCashflow { get; set; } = false;       
        public bool IsCritical { get; set; } = false;

        // ========== METADATA ==========

        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
    }


    /// Represents detailed cashflow summary for entire projection period

    [Dapper.Contrib.Extensions.Table("tblCashflowSummary")]
    public class CashflowSummary
    {
        [Dapper.Contrib.Extensions.Key] public long CashflowSummaryId { get; set; }
        [Required] public long AssessmentId { get; set; }

        [Dapper.Contrib.Extensions.Computed]
        [ForeignKey("AssessmentId")] public virtual Assessment Assessment { get; set; }

        // ========== ANNUAL TOTALS ==========

        [Column(TypeName = "decimal(18,2)")] public decimal TotalAnnualIncome { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal TotalAnnualExpense { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal TotalAnnualNetCashflow { get; set; } = 0;

        // ========== CASHFLOW HEALTH METRICS ==========


        [Column(TypeName = "decimal(18,2)")] public decimal MinimumCashBalance { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal MaximumCashBalance { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")] public decimal AverageMonthlyNetCashflow { get; set; } = 0;
        public int MonthsWithNegativeCashflow { get; set; } = 0;   
        public int CriticalMonthsCount { get; set; } = 0;      
        public decimal CashflowRunway { get; set; } = 0;

        // ========== RATIOS & INDICATORS ==========
          
        [Column(TypeName = "decimal(5,2)")] public decimal OperatingMarginRatio { get; set; } = 0;
        [Column(TypeName = "decimal(5,2)")] public decimal ExpenseRatio { get; set; } = 0;
        public CashflowHealthStatus HealthStatus { get; set; } = CashflowHealthStatus.Healthy;
        public bool IsSustainable { get; set; } = true;

        // ========== METADATA ==========
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
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

        public decimal GrossVAT { get; set; }
        public decimal NetVAT { get; set; }

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
