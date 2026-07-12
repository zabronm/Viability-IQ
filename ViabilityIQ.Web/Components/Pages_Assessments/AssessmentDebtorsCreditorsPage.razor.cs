using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents.WorkingCapital;


namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentDebtorsCreditorsPage
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }


        //---------------------------------------------------------
        // Alert
        //---------------------------------------------------------

        private bool blAlert = true;

        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;

        private string AlertHeading = "Working Capital";

        private string AlertMessage = "Collection and payment profiles determine when cash moves through the projected cashbook. Monthly receipts and payments are calculated automatically from your forecasts.";


        // 12-month calendar timeline matching your table headers
        private readonly string[] timeline = { "Sept-99", "Oct-99", "Nov-99", "Dec-99", "Jan-00", "Feb-00", "Mar-00", "Apr-00", "May-00", "Jun-00", "Jul-00", "Aug-00" };

        public decimal DebtorsBalance => 205000;
        public int DebtorDays => 42;
        public decimal CollectionEfficiency => 88.5m;
        public decimal HighRiskDebtors => DebtorsRows.Sum(r => r.MonthlyAllocations.Skip(3).Sum()); // Based on index 3+ (90 days+)



        public AgingProfileConfiguration DebtorsProfile { get; set; } = new()
        {
            Percent0To30Days = 50,
            Percent30To60Days = 20,
            Percent60To90Days = 10,
            Percent90To120Days = 10,
            PercentOver120Days = 10
        };

        public List<DebtorRow> DebtorsRows { get; set; } = new();

        protected override void OnInitialized()
        {
            LoadDebtorsRows();
            RecalculateAllocations();
        }

        public void UpdateProfile(string range, ChangeEventArgs e)
        {
            decimal val = decimal.TryParse(e.Value?.ToString(), out var p) ? p : 0;
            SetProfileValue(range, val);

            // Enforce sum of 100: 0-30 days is the balancer
            decimal others = DebtorsProfile.Percent30To60Days + DebtorsProfile.Percent60To90Days +
                             DebtorsProfile.Percent90To120Days + DebtorsProfile.PercentOver120Days;
            DebtorsProfile.Percent0To30Days = Math.Max(0, 100 - others);

            RecalculateAllocations();
            StateHasChanged();
        }

        private void RecalculateAllocations()
        {
            foreach (var row in DebtorsRows)
            {
                row.MonthlyAllocations = new List<decimal>(new decimal[12]);
                int idx = Array.IndexOf(timeline, row.SourceMonth);

                if (idx != -1)
                {
                    // Distribute across the timeline columns based on aging tier[cite: 3]
                    if (idx + 0 < 12) row.MonthlyAllocations[idx + 0] = row.InvoicedAmount * (DebtorsProfile.Percent0To30Days / 100);
                    if (idx + 1 < 12) row.MonthlyAllocations[idx + 1] = row.InvoicedAmount * (DebtorsProfile.Percent30To60Days / 100);
                    if (idx + 2 < 12) row.MonthlyAllocations[idx + 2] = row.InvoicedAmount * (DebtorsProfile.Percent60To90Days / 100);
                    if (idx + 3 < 12) row.MonthlyAllocations[idx + 3] = row.InvoicedAmount * (DebtorsProfile.Percent90To120Days / 100);
                    if (idx + 4 < 12) row.MonthlyAllocations[idx + 4] = row.InvoicedAmount * (DebtorsProfile.PercentOver120Days / 100);
                }
            }
        }

        private void LoadDebtorsRows()
        {
            DebtorsRows = new List<DebtorRow>
            {
                new() { SourceMonth = "Sept-99", InvoicedAmount = 2000 },
                new() { SourceMonth = "Oct-99", InvoicedAmount = 5600 },
                new() { SourceMonth = "Nov-99", InvoicedAmount = 15400 },
                new() { SourceMonth = "Dec-99", InvoicedAmount = 2546 },
                new() { SourceMonth = "Jan-00", InvoicedAmount = 2560 }
            };
        }

        private void SetProfileValue(string range, decimal val)
        {
            if (range == "30-60") DebtorsProfile.Percent30To60Days = val;
            else if (range == "60-90") DebtorsProfile.Percent60To90Days = val;
            else if (range == "90-120") DebtorsProfile.Percent90To120Days = val;
            else if (range == "120+") DebtorsProfile.PercentOver120Days = val;
        }

        public decimal GetProfileValue(string range) => range switch
        {
            "0-30" => DebtorsProfile.Percent0To30Days,
            "30-60" => DebtorsProfile.Percent30To60Days,
            "60-90" => DebtorsProfile.Percent60To90Days,
            "90-120" => DebtorsProfile.Percent90To120Days,
            "120+" => DebtorsProfile.PercentOver120Days,
            _ => 0
        };


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await RenderChart();
        }

        private async Task RenderChart()
        {
            // Check if the function exists on the window object before calling it
            bool exists = await JS.InvokeAsync<bool>("eval", "typeof window.renderDebtorsPieChart === 'function'");

            if (exists)
            {
                var data = new[] { DebtorsProfile.Percent0To30Days, DebtorsProfile.Percent30To60Days, DebtorsProfile.Percent60To90Days, DebtorsProfile.Percent90To120Days, DebtorsProfile.PercentOver120Days };
                await JS.InvokeVoidAsync("renderDebtorsPieChart", (object)data);
            }
            else
            {
                // Log to console if still failing so you can debug
                await JS.InvokeVoidAsync("console.error", "renderDebtorsPieChart is still undefined");
            }
        }
    }

    public class AgingProfileConfiguration
    {
        public decimal Percent0To30Days { get; set; }
        public decimal Percent30To60Days { get; set; }
        public decimal Percent60To90Days { get; set; }
        public decimal Percent90To120Days { get; set; }
        public decimal PercentOver120Days { get; set; }
    }

    public class DebtorRow
    {
        public string SourceMonth { get; set; } = "";
        public decimal InvoicedAmount { get; set; }
        public List<decimal> MonthlyAllocations { get; set; } = new();
        public decimal RowTotal => MonthlyAllocations.Sum();
    }


}







