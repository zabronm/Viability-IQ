using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Reflection.Metadata;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentReportsPage
    {
        [Parameter] public long AssessmentId { get; set; }

        private Dictionary<string, List<ReportDefinition>> GroupedReports { get; set; } = new();
        private ReportDefinition? SelectedReport { get; set; }
        private Dictionary<string, string> ParameterValues { get; set; } = new();

        private bool IsLoading { get; set; } = false;
        private bool HasGenerated { get; set; } = false;

        // Strong typed model representation for the data table
        private List<ReportRowModel> MockReportItems { get; set; } = new();

        protected override void OnInitialized()
        {
            LoadReportMetadataDefinitions();
        }

        private void LoadReportMetadataDefinitions()
        {
            var rawList = new List<ReportDefinition>
        {
            new ReportDefinition
            {
                Id = "SALES_PERF",
                Name = "Sales Performance Ledger",
                Category = "Operational Trading & Performance",
                Description = "Evaluates chronological trading margins, volume velocities, and turnover benchmarks.",
                Parameters = new() {
                    new() { Key = "StartDate", Label = "Start Date Range", Type = ReportParameterType.Date },
                    new() { Key = "EndDate", Label = "End Date Range", Type = ReportParameterType.Date }
                }
            },
            new ReportDefinition
            {
                Id = "EXPENSE_ANALYSIS",
                Name = "Operational Expense Breakdown",
                Category = "Operational Trading & Performance",
                Description = "Granular distribution trace mapping across cost centers and dynamic discretionary spending outlays.",
                Parameters = new() {
                    new() { Key = "StartDate", Label = "Start Date Range", Type = ReportParameterType.Date },
                    new() { Key = "EndDate", Label = "End Date Range", Type = ReportParameterType.Date }
                }
            },
            new ReportDefinition
            {
                Id = "BALANCE_SHEET",
                Name = "Statement of Financial Position (Balance Sheet)",
                Category = "Financial Position & Structure",
                Description = "Captures static snapshots of corporate infrastructure equity balances, assets, and operational liabilities.",
                Parameters = new() {
                    new() { Key = "AsAtDate", Label = "Reporting Threshold Date (As At)", Type = ReportParameterType.Date }
                }
            },
            new ReportDefinition
            {
                Id = "VAT_LEDGER",
                Name = "Value Added Tax (VAT) Reconciliation Matrix",
                Category = "Statutory Compliance & Audits",
                Description = "Validates output statutory collection logs against verifiable baseline vendor input expense claims.",
                Parameters = new() {
                    new() { Key = "PeriodYear", Label = "Tax Assessment Year", Type = ReportParameterType.Number },
                    new() { Key = "FilingPeriod", Label = "Period Reference Code", Type = ReportParameterType.Text }
                }
            }
        };

            // Regroup into clean categorizations for the dropdown loop
            GroupedReports = rawList.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.ToList());
        }

        private void OnReportSelected(ChangeEventArgs e)
        {
            HasGenerated = false;
            ParameterValues.Clear();

            string selectedId = e.Value?.ToString() ?? string.Empty;
            SelectedReport = GroupedReports.Values.SelectMany(r => r).FirstOrDefault(r => r.Id == selectedId);

            if (SelectedReport != null)
            {
                foreach (var param in SelectedReport.Parameters)
                {
                    ParameterValues[param.Key] = param.DefaultValue ?? string.Empty;
                }
            }
        }

        // Improvement 3: Check that all required parameters possess entered text values
        private bool IsConfigurationValid()
        {
            if (SelectedReport == null) return false;
            foreach (var param in SelectedReport.Parameters)
            {
                if (param.IsRequired && (!ParameterValues.ContainsKey(param.Key) || string.IsNullOrWhiteSpace(ParameterValues[param.Key])))
                {
                    return false;
                }
            }
            return true;
        }

        // Improvement 2: Fast Quick-Click Calculation Procedures
        private void ApplyCurrentMonthRange()
        {
            ParameterValues["StartDate"] = new DateTime(2026, 06, 01).ToString("yyyy-MM-dd");
            ParameterValues["EndDate"] = new DateTime(2026, 06, 30).ToString("yyyy-MM-dd");
        }

        private void ApplyCurrentQuarterRange()
        {
            ParameterValues["StartDate"] = new DateTime(2026, 04, 01).ToString("yyyy-MM-dd");
            ParameterValues["EndDate"] = new DateTime(2026, 06, 30).ToString("yyyy-MM-dd");
        }

        private void ApplyFullFinancialYear()
        {
            ParameterValues["StartDate"] = new DateTime(2026, 01, 01).ToString("yyyy-MM-dd");
            ParameterValues["EndDate"] = new DateTime(2026, 12, 31).ToString("yyyy-MM-dd");
        }

        private string GetParamValue(string key) => ParameterValues.ContainsKey(key) ? ParameterValues[key] : string.Empty;
        private void SetParamValue(string key, string? value) => ParameterValues[key] = value ?? string.Empty;

        private void ClearFilters()
        {
            HasGenerated = false;
            if (SelectedReport != null)
            {
                foreach (var param in SelectedReport.Parameters) ParameterValues[param.Key] = string.Empty;
            }
        }

        private async Task GenerateReport()
        {
            IsLoading = true;
            HasGenerated = false;

            await Task.Delay(600); // Compute cycle lag mock

            // Seed concrete typed list models to bind straight into downstream exports
            MockReportItems = new List<ReportRowModel>
        {
            new() { LedgerCode = "GL-4001-Z01", Description = "Primary System Stream Balance Allocation", BaseAmount = 1245000.00, Variance = 42500.00 },
            new() { LedgerCode = "GL-5082-A12", Description = "Fixed Indirect Overhead Adjustments", BaseAmount = 412800.00, Variance = -11200.00 },
            new() { LedgerCode = "GL-7120-X09", Description = "Logistical Distribution Clearing Index", BaseAmount = 184500.00, Variance = 3400.00 }
        };

            IsLoading = false;
            HasGenerated = true;
        }

        // Secondary structural helper wrappers
        public enum ReportParameterType { Date, Number, Text }

        public class ParameterDefinition
        {
            public string Key { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public ReportParameterType Type { get; set; }
            public bool IsRequired { get; set; } = true;
            public string? DefaultValue { get; set; }
        }

        public class ReportDefinition
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<ParameterDefinition> Parameters { get; set; } = new();
        }

        public class ReportRowModel
        {
            public string LedgerCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public double BaseAmount { get; set; }
            public double Variance { get; set; }
        }
    }
}
