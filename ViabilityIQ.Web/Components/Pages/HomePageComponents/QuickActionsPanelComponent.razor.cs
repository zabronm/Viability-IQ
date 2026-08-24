using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{

    /// QuickActionsPanel displays primary action buttons for common tasks
    /// Visibility of buttons is controlled by user role

    public partial class QuickActionsPanelComponent : ComponentBase
    {
        #region Injected Dependencies

        [Inject] public ILogger<QuickActionsPanelComponent> Logger { get; set; }
        [Inject]        public NavigationManager NavigationManager { get; set; } = default!;
        #endregion

        #region Parameters

        /// <summary>
        /// Callback when user clicks an action button
        /// Parameter is an action ID (1=NewAssessment, 2=NewBusiness, 3=NewClient, 4=ViewActivityLog)
        /// </summary>
        [Parameter]
        public EventCallback<int> OnActionClick { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Handle action button click
        /// Action IDs:
        /// 1 = New Assessment
        /// 2 = New Business
        /// 3 = New Client
        /// 4 = View Activity Log
        /// </summary>
        public async Task HandleActionClick(int actionId)
        {

            Logger.LogInformation(
                "Quick action clicked: Action ID {ActionId}",
                actionId);

            switch (actionId)
            {
                case 1:
                    NavigationManager.NavigateTo("/settings/assessments");
                    break;

                case 2:
                    NavigationManager.NavigateTo("/settings/businesses");
                    break;

                case 3:
                    NavigationManager.NavigateTo("/settings/client-contacts");
                    break;

                case 4:
                    NavigationManager.NavigateTo("/settings/settings");
                    break;

                default:
                    Logger.LogWarning(
                        "Unknown quick action ID: {ActionId}",
                        actionId);
                    break;
            }

            // Notify parent component if required
            //if (OnActionClick.HasDelegate)
            //{
            //    await OnActionClick.InvokeAsync(actionId);
            //}
        }

        #endregion
    }
}
