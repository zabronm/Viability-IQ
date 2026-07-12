using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents.WorkingCapital
{
    public partial class WorkingCapitalDistributionComponent : ComponentBase
    {
        //---------------------------------------------------------
        // Parameters
        //---------------------------------------------------------

        [Parameter]
        public WorkingCapitalModule Module { get; set; } = new();

        //---------------------------------------------------------
        // Computed CSS
        //---------------------------------------------------------

        private string HeaderCss =>
            Module.Theme?.ToLower() == "debtors"
                ? "wcm-header-debtors"
                : "wcm-header-creditors";

        private string PanelCss =>
            Module.Theme?.ToLower() == "debtors"
                ? "wcm-debtors"
                : "wcm-creditors";

        private string ProfileInputCss =>
            IsProfileValid
                ? "form-control text-center tile-input"
                : "form-control text-center tile-input invalid-profile";

        //---------------------------------------------------------
        // Validation
        //---------------------------------------------------------

        private bool IsProfileValid =>
            Math.Abs(Module.Profile.Total - 100m) < 0.001m;

        //---------------------------------------------------------
        // Lifecycle
        //---------------------------------------------------------

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (Module == null)
                Module = new WorkingCapitalModule();

            if (Module.Profile == null)
                Module.Profile = new WorkingCapitalProfile();

            if (Module.MonthlyValues == null)
                Module.MonthlyValues = new List<WorkingCapitalMonthlyValue>();

            if (Module.Distribution == null)
                Module.Distribution = new List<WorkingCapitalDistributionRow>();

            if (Module.DistributionTotals == null)
                Module.DistributionTotals = new List<decimal>();

            if (Module.MonthlyTotals == null)
                Module.MonthlyTotals = new List<decimal>();

            if (Module.Summary == null)
                Module.Summary = new WorkingCapitalSummary();
        }

        //---------------------------------------------------------
        // Recalculate
        //---------------------------------------------------------

        private void Recalculate()
        {
            WorkingCapitalEngine.GenerateDistribution(Module);

            StateHasChanged();
        }

        //---------------------------------------------------------
        // Reset Profile
        //---------------------------------------------------------

        private void ResetProfile()
        {
            Module.Profile.Days0To30 = 60;
            Module.Profile.Days30To60 = 20;
            Module.Profile.Days60To90 = 14;
            Module.Profile.Days90To120 = 6;

            Recalculate();
        }

        //---------------------------------------------------------
        // Helpers
        //---------------------------------------------------------

        private string FormatCurrency(decimal value)
        {
            return value.ToString("N0");
        }

        private string FormatPercentage(decimal value)
        {
            return value.ToString("N2") + "%";
        }

        private string FormatDays(decimal value)
        {
            return value.ToString("N1");
        }
    }
}