using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class WhatIfPage
    {
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Parameter] public long AssessmentId { get; set; }

        private long ActiveAssessmentId { get; set; }

        // Dashboard State Controls & Data Properties
        protected bool IsProfitable { get; set; } = true;
        protected List<MonthPnLSummary> MonthlyPerformanceMockData { get; set; } = new();

        // Evaluates dynamically using your logic thresholds: red (<40), yellow (40-49), green (50-55), navy(>55)
        protected double SimulatedNetMarginPercentage { get; set; } = 46.1;

        protected string ProfitStyleClass => SimulatedNetMarginPercentage switch
        {
            < 40.0 => "border-left-danger-dynamic",
            >= 40.0 and < 50.0 => "border-left-warning-dynamic",
            >= 50.0 and <= 55.0 => "border-left-success-dynamic",
            _ => "border-left-navy-dynamic"
        };

        protected override void OnInitialized()
        {
            if (sessionService?.AssessmentId != null)
            {
                ActiveAssessmentId = sessionService.AssessmentId.Value;
                InitializeDashboardMockDataset();
            }
            else
            {
                _Toast!.ShowError("Case number is unknown, please restart your application.");
            }
            
        }

        private void InitializeDashboardMockDataset()
        {
            MonthlyPerformanceMockData = new List<MonthPnLSummary>
            {
                new() { Month = "Jan", Value = 45, IsProfit = true },
                new() { Month = "Feb", Value = 55, IsProfit = true },
                new() { Month = "Mar", Value = 38, IsProfit = true },
                new() { Month = "Apr", Value = 22, IsProfit = false },
                new() { Month = "May", Value = 62, IsProfit = true },
                new() { Month = "Jun", Value = 78, IsProfit = true },
                new() { Month = "Jul", Value = 85, IsProfit = true },
                new() { Month = "Aug", Value = 92, IsProfit = true },
                new() { Month = "Sep", Value = 64, IsProfit = true },
                new() { Month = "Oct", Value = 15, IsProfit = false },
                new() { Month = "Nov", Value = 110, IsProfit = true },
                new() { Month = "Dec", Value = 145, IsProfit = true }
            };
        }

        public class MonthPnLSummary
        {
            public string Month { get; set; } = string.Empty;
            public int Value { get; set; }
            public bool IsProfit { get; set; }
        }
    }
}