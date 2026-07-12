using System.Collections.Generic;
using System.Linq;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents.WorkingCapital
{
    /// <summary>
    /// Represents an entire Working Capital module
    /// (Debtors or Creditors).
    /// </summary>
    public class WorkingCapitalModule
    {
        //--------------------------------------------------------
        // General
        //--------------------------------------------------------

        public string Title { get; set; } = "";

        public string Icon { get; set; } = "";

        public string Theme { get; set; } = "";

        //--------------------------------------------------------
        // Collection / Payment Profile
        //--------------------------------------------------------

        public WorkingCapitalProfile Profile { get; set; } = new();

        //--------------------------------------------------------
        // Monthly Source Values
        //--------------------------------------------------------

        public List<WorkingCapitalMonthlyValue> MonthlyValues { get; set; }
            = new();

        //--------------------------------------------------------
        // Distribution Matrix
        //--------------------------------------------------------

        public List<WorkingCapitalDistributionRow> Distribution { get; set; }
            = new();

        //--------------------------------------------------------
        // Summary
        //--------------------------------------------------------

        public WorkingCapitalSummary Summary { get; set; }
            = new();

        //--------------------------------------------------------
        // Monthly Totals
        //--------------------------------------------------------

        public List<decimal> MonthlyTotals { get; set; }
            = new();

        //--------------------------------------------------------
        // Distribution Totals
        //--------------------------------------------------------

        public List<decimal> DistributionTotals { get; set; }
            = new();

        public decimal TotalInvoiced { get; set; }

        public decimal TotalDistributed { get; set; }

        //--------------------------------------------------------
        // Grand Total
        //--------------------------------------------------------

        public decimal GrandTotal => MonthlyTotals.Sum();
    }

    //======================================================================
    // MONTHLY SOURCE VALUES
    //======================================================================

    public class WorkingCapitalMonthlyValue
    {
        public int PeriodNo { get; set; }

        public string Month { get; set; } = "";

        /// <summary>
        /// Monthly Sales (Debtors)
        /// or Monthly Purchases (Creditors)
        /// </summary>
        public decimal InvoicedAmount { get; set; }

        public decimal ActualReceipts { get; set; }

        public decimal OutstandingBalance { get; set; }
    }

    //======================================================================
    // COLLECTION / PAYMENT PROFILE
    //======================================================================

    public class WorkingCapitalProfile
    {
        public decimal Days0To30 { get; set; }

        public decimal Days30To60 { get; set; }

        public decimal Days60To90 { get; set; }

        public decimal Days90To120 { get; set; }

        public decimal Total =>
            Days0To30 +
            Days30To60 +
            Days60To90 +
            Days90To120;
    }

    //======================================================================
    // DISTRIBUTION MATRIX ROW
    //======================================================================

    public class WorkingCapitalDistributionRow
    {
        public string Month { get; set; } = "";

        /// <summary>
        /// Invoice value for this month.
        /// </summary>
        public decimal Invoiced { get; set; }

        /// <summary>
        /// Distributed values.
        /// One value per ageing bucket.
        /// </summary>
        public List<decimal> Values { get; set; }
            = new();

        public decimal Total => Values.Sum();
    }

    //======================================================================
    // KPI SUMMARY
    //======================================================================

    public class WorkingCapitalSummary
    {
        /// <summary>
        /// Outstanding Debtors / Creditors.
        /// </summary>
        public decimal Outstanding { get; set; }

        /// <summary>
        /// Debtor Days / Creditor Days.
        /// </summary>
        public decimal Days { get; set; }

        /// <summary>
        /// Outstanding as % of Sales/Purchases.
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Annual Sales/Purchases.
        /// </summary>
        public decimal AnnualMovement { get; set; }
    }
}