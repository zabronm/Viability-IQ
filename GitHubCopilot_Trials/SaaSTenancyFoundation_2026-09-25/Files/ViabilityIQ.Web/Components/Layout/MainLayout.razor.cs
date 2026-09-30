using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject] private OffCanvasStateService OffCanvasService { get; set; } = default!;
        [Inject] private ISessionService SessionService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IAuthenticationService AuthenticationService { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> AssessmentRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Business> BusinessRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Client> ClientRepository { get; set; } = default!;
        [Inject] private INotificationItemRepository NotificationRepository { get; set; } = default!;
        [Inject] private ITenantService TenantService { get; set; } = default!;
        [Inject] private ILogger<MainLayout> Logger { get; set; } = default!;

        private bool isSidebarCollapsed = false;
        private void ToggleSidebar() => isSidebarCollapsed = !isSidebarCollapsed;

        private ZabOffCanvas? OffCanvasControlRef;
        private string currentTitle = string.Empty;
        private int currentWidth = 550;
        private Type? dynamicComponentType;
        private Dictionary<string, object?> dynamicParameters = new();
        private bool isCanvasOpen = false;
        private int AssessmentCount { get; set; }
        private int BusinessCount { get; set; }
        private int ClientCount { get; set; }
        private int UnreadNotificationCount { get; set; }
        private bool isEstablishingUserSession;

        protected override void OnInitialized()
        {
            OffCanvasService.OnShow += HandleCanvasShowAsync;
            OffCanvasService.OnClose += HandleCanvasCloseAsync;
            OffCanvasService.OnSave += HandleCanvasSaveAsync;
            SessionService.OnSessionChanged += HandleSessionChanged;
        }

        protected override async Task OnInitializedAsync()
        {
            await EnsureAuthenticatedSessionAsync();
            await LoadNavigationCountsAsync();
        }

        private async Task HandleCanvasShowAsync(CanvasRequest request)
        {
            currentTitle = request.Title;

            if (request.Width != null)
            {
                var widthStr = request.Width.ToString();
                if (!string.IsNullOrEmpty(widthStr) && int.TryParse(widthStr.Replace("px", "").Trim(), out var parsedWidth))
                {
                    currentWidth = parsedWidth;
                }
            }

            dynamicComponentType = request.ComponentType;
            dynamicParameters = OffCanvasStateService.ConvertToDictionary(request.Parameters);
            isCanvasOpen = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleCanvasCloseAsync()
        {
            isCanvasOpen = false;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(300);
            dynamicComponentType = null;
            dynamicParameters.Clear();
            await LoadNavigationCountsAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleCanvasSaveAsync(SaveResult result)
        {
            if (!result.RefreshDashboard && !result.RefreshKPIs && !result.RefreshSummary)
            {
                return;
            }

            await LoadNavigationCountsAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadNavigationCountsAsync()
        {
            var userId = SessionService.UserId;
            if (!SessionService.IsAuthenticated || userId <= 0)
            {
                AssessmentCount = 0;
                BusinessCount = 0;
                ClientCount = 0;
                UnreadNotificationCount = 0;
                return;
            }

            try
            {
                var assessmentTask = AssessmentRepository.CountAsync(item => item.CreatedBy == userId);
                var businessTask = BusinessRepository.CountAsync(item => item.CreatedBy == userId);
                var clientTask = ClientRepository.CountAsync(item => item.CreatedBy == userId);
                var notificationTask = NotificationRepository.GetUnreadCountAsync(userId);

                await Task.WhenAll(assessmentTask, businessTask, clientTask, notificationTask);

                AssessmentCount = await assessmentTask;
                BusinessCount = await businessTask;
                ClientCount = await clientTask;
                UnreadNotificationCount = await notificationTask;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Unable to load navigation counts for user {UserId}.", userId);
            }
        }

        private async Task EnsureAuthenticatedSessionAsync()
        {
            if (SessionService.IsAuthenticated && SessionService.UserId > 0)
            {
                return;
            }

            try
            {
                var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                if (authenticationState.User.Identity?.IsAuthenticated != true)
                {
                    Logger.LogWarning("Navigation counts were not loaded because the user is not authenticated.");
                    return;
                }

                var applicationUser = await AuthenticationService.GetCurrentUserAsync(authenticationState.User);
                if (applicationUser is null)
                {
                    Logger.LogWarning("Navigation counts were not loaded because the authenticated user profile could not be resolved.");
                    return;
                }

                var displayName = string.Join(
                    " ",
                    new[] { applicationUser.FirstName, applicationUser.LastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value)));

                isEstablishingUserSession = true;
                SessionService.EstablishUserSession(
                    applicationUser.Id,
                    string.IsNullOrWhiteSpace(displayName)
                        ? applicationUser.UserName ?? applicationUser.Email ?? string.Empty
                        : displayName,
                    applicationUser.Email ?? string.Empty,
                    0,
                    applicationUser.BranchId ?? 0,
                    applicationUser.ProvinceId ?? 0);

                var tenant = await TenantService.GetDefaultTenantAsync(applicationUser.Id);
                if (tenant is not null)
                {
                    SessionService.SetActiveTenant(
                        tenant.TenantId,
                        tenant.TenantName,
                        tenant.TenantType,
                        tenant.PlanCode,
                        tenant.SubscriptionStatus,
                        tenant.MembershipId,
                        tenant.IsOwner);
                }
                else
                {
                    Logger.LogWarning(
                        "Authenticated user {UserId} has no active tenant membership.",
                        applicationUser.Id);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Unable to establish the authenticated session for navigation counts.");
            }
            finally
            {
                isEstablishingUserSession = false;
            }
        }

        private void HandleSessionChanged()
        {
            if (isEstablishingUserSession)
            {
                return;
            }

            _ = InvokeAsync(async () =>
            {
                await LoadNavigationCountsAsync();
                StateHasChanged();
            });
        }

        private static string FormatCount(int count) => count > 99 ? "99+" : count.ToString();

        public void Dispose()
        {
            OffCanvasService.OnShow -= HandleCanvasShowAsync;
            OffCanvasService.OnClose -= HandleCanvasCloseAsync;
            OffCanvasService.OnSave -= HandleCanvasSaveAsync;
            SessionService.OnSessionChanged -= HandleSessionChanged;
        }
    }
}