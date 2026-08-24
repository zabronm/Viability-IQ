using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Web.Models.Dashboard;


namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{
    /// KPIWidgetsSection displays a grid of 6 KPI metric cards
    /// - 3 personal metrics (Active, Completed, Pending)
    /// - 3 organizational metrics (Workload, Clients, Branch)   
    public partial class KPIWidgetsSectionComponent : ComponentBase
    {
        /// <summary>
        /// KPI metrics data to display
        /// </summary>
        [Parameter]
        public KPIMetricsModel KPIMetrics { get; set; }

        /// <summary>
        /// Callback when user clicks on a KPI card for drill-down
        /// Parameter: kpiType (e.g., "ActiveAssessments", "CompletedAssessments", etc.)
        /// </summary>
        [Parameter]
        public EventCallback<string> OnKPIDrill { get; set; }

        /// <summary>
        /// Handle KPI card click - invoke drill-down callback
        /// </summary>
        private async Task HandleDrill(string kpiType)
        {
            if (OnKPIDrill.HasDelegate)
            {
                await OnKPIDrill.InvokeAsync(kpiType);
            }
        }
    }
}



