using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class WhatIfPage
    {
        [Parameter] public long AssessmentId { get; set; }
  
        // Dashboard State Controls & Data Properties
        protected bool IsProfitable { get; set; } = true;
        protected List<MonthPnLSummary> MonthlyPerformanceMockData { get; set; } = new();

        protected override void OnInitialized()
        {
            InitializeDashboardMockDataset();
        }

        private void InitializeDashboardMockDataset()
        {
            // Seed a standard 12-month sequence configuration matrix for the P&L chart visualization wrapper
            MonthlyPerformanceMockData = new List<MonthPnLSummary>
            {
                new() { Month = "Jan", Value = 45, IsProfit = true },
                new() { Month = "Feb", Value = 55, IsProfit = true },
                new() { Month = "Mar", Value = 38, IsProfit = true },
                new() { Month = "Apr", Value = 22, IsProfit = false }, // Simulated operating drop down cycle
                new() { Month = "May", Value = 62, IsProfit = true },
                new() { Month = "Jun", Value = 78, IsProfit = true },
                new() { Month = "Jul", Value = 85, IsProfit = true },
                new() { Month = "Aug", Value = 92, IsProfit = true },
                new() { Month = "Sep", Value = 64, IsProfit = true },
                new() { Month = "Oct", Value = 15, IsProfit = false }, // Minor overhead shock event
                new() { Month = "Nov", Value = 110, IsProfit = true },
                new() { Month = "Dec", Value = 145, IsProfit = true }
            };
        }

        // Inner supporting model blueprint for structured dataset layout loops
        public class MonthPnLSummary
        {
            public string Month { get; set; } = string.Empty;
            public int Value { get; set; }
            public bool IsProfit { get; set; }
        }
    }
}
