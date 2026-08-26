using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Services;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{
    /// <summary>
    /// QuickActionsPanel displays primary action buttons for common tasks
    /// Opens form components via OffCanvas service
    /// </summary>
    public partial class QuickActionsPanelComponent : ComponentBase
    {
        #region Injected Dependencies

        [Inject] public ILogger<QuickActionsPanelComponent> Logger { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;

        #endregion

        #region Parameters

        /// <summary>
        /// Callback when user clicks an action button
        /// Parameter is an action ID (1=NewAssessment, 2=NewBusiness, 3=NewClient, 4=ViewSettings)
        /// </summary>
        [Parameter]
        public EventCallback<int> OnActionClick { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Handle action button click
        /// Action IDs:
        /// 1 = New Assessment (opens AssessmentsFormComponent)
        /// 2 = New Business (opens BusinessFormComponent)
        /// 3 = New Client (opens ClientFormComponent)
        /// 4 = Settings (navigates to settings page)
        /// </summary>
        public async Task HandleActionClick(int actionId)
        {
            Logger.LogInformation(
                "Quick action clicked: Action ID {ActionId}",
                actionId);

            switch (actionId)
            {
                case 1:
                    // ✅ Open New Assessment form via service
                    await OpenNewAssessmentForm();
                    break;

                case 2:
                    // ✅ Open New Business form via service
                    await OpenNewBusinessForm();
                    break;

                case 3:
                    // ✅ Open New Client form via service
                    await OpenNewClientForm();
                    break;

                case 4:
                    NavigationManager.NavigateTo("/settings");
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

        /// <summary>
        /// Open New Assessment form via OffCanvas service
        /// </summary>
        private async Task OpenNewAssessmentForm()
        {
            try
            {
                await OffcanvasService!.ShowAsync(new CanvasRequest
                {
                    Title = "Initiate New Assessment Case",
                    Width = 400,
                    ComponentType = typeof(AssessmentsFormComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "AssessmentId", 0L }
                    },
                    ResultCallback = async (result) => await HandleFormResult(result, "Assessment")
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error opening new assessment form");
            }
        }

        /// <summary>
        /// Open New Business form via OffCanvas service
        /// </summary>
        private async Task OpenNewBusinessForm()
        {
            try
            {
                await OffcanvasService!.ShowAsync(new CanvasRequest
                {
                    Title = "Add Business Details",
                    Width = 550,
                    ComponentType = typeof(BusinessFormComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "BusinessId", 0L }
                    },
                    ResultCallback = async (result) => await HandleFormResult(result, "Business")
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error opening new business form");
            }
        }

        /// <summary>
        /// Open New Client form via OffCanvas service
        /// </summary>
        private async Task OpenNewClientForm()
        {
            try
            {
                await OffcanvasService!.ShowAsync(new CanvasRequest
                {
                    Title = "Add Client Details",
                    Width = 550,
                    ComponentType = typeof(ClientFormComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "ClientId", 0L }
                    },
                    ResultCallback = async (result) => await HandleFormResult(result, "Client")
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error opening new client form");
            }
        }

        /// <summary>
        /// Handle form result callback
        /// </summary>
        private async Task HandleFormResult(SaveResult result, string entityType)
        {
            if (result.Success)
            {
                Logger.LogInformation(
                    "Quick action form completed successfully: {EntityType}",
                    entityType);
            }
            else
            {
                Logger.LogWarning(
                    "Quick action form failed: {EntityType} - {Message}",
                    entityType,
                    result.Message);
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}