using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Web.Extensions;
using ViabilityIQ.Web.Models.Dashboard;


namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{

    /// SystemInsightsSection displays analytics, performance metrics,
    /// status distribution charts, and top performers leaderboard
    /// with export capability

    public partial class SystemInsightsSectionComponent : ComponentBase
    {

        #region Injected Dependencies

        [Inject] public ILogger<SystemInsightsSectionComponent> Logger { get; set; }

        #endregion

        #region Parameters

        /// <summary>
        /// The insights and analytics data to display
        /// </summary>
        [Parameter]
        public InsightsModel Insights { get; set; }

        /// <summary>
        /// Callback when user clicks export button
        /// exportTypeId: 1=CompletionRate, 2=AvgCompletionTime, 3=StatusDistribution, 4=TopPerformers
        /// formatId: 1=Excel, 2=PDF
        /// </summary>
        [Parameter]
        public EventCallback<(int exportTypeId, int formatId)> OnExport { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Handle export button click
        /// exportTypeId: 1=CompletionRate, 2=AvgCompletionTime, 3=StatusDistribution, 4=TopPerformers
        /// formatId: 1=Excel, 2=PDF
        /// </summary>
        public async Task HandleExport(int exportTypeId, int formatId)
        {
            Logger.LogInformation("Export requested: Export Type ID {ExportTypeId}, Format ID {FormatId}",
                exportTypeId, formatId);

            if (OnExport.HasDelegate)
            {
                await OnExport.InvokeAsync((exportTypeId, formatId));
            }
        }

        #endregion
    }
}
