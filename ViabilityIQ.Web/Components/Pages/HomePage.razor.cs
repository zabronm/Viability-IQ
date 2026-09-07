using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Infrastructure.Repositories.HomePageRepositories;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Web.Extensions;
using ViabilityIQ.Web.Models.Dashboard;


namespace ViabilityIQ.Web.Components.Pages
{
    public partial class HomePage : ComponentBase
    {
        #region Injected Dependencies

        [Inject]
        public CustomAuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Inject]
        public IAuthenticationService AuthService { get; set; }

        [Inject]
        public IDashboardDataService DashboardDataService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public ILogger<HomePage> Logger { get; set; }

        #endregion

        #region Properties

       
        /// Currently authenticated user
       
        public ApplicationUser CurrentUser { get; set; }

       
        /// Current branch name
       
        public string CurrentBranch { get; set; } = "Default Branch";

       
        /// Last login date/time for the user
       
        public DateTime LastLoginDate { get; set; }

       
        /// Loading state flag
       
        public bool IsLoading { get; set; } = true;

        #endregion

        #region Dashboard Data Properties

       
        /// KPI metrics (Active, Completed, Pending, etc.)
       
        public KPIMetricsModel KPIData { get; set; }

       
        /// Recent activities log
       
        public List<ActivityLogModel> RecentActivities { get; set; } = new List<ActivityLogModel>();

       
        /// Recent assessments
       
        public List<AssessmentModel> RecentAssessments { get; set; } = new List<AssessmentModel>();

       
        /// Urgent and upcoming assessment alerts
       
        public AlertsModel AlertsData { get; set; }

       
        /// System announcements
       
        public List<SystemAnnouncementModel> SystemAnnouncements { get; set; } = new List<SystemAnnouncementModel>();

       
        /// Insights and analytics data
       
        public InsightsModel InsightsData { get; set; }

        #endregion

        #region Lifecycle Methods

       
        /// Component initialization - Load user and dashboard data
       
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger.LogInformation("HomePage initializing...");
                IsLoading = true;

                // Load user data from authentication state
                await LoadUserData();

                // Load all dashboard data only if CurrentUser is loaded
                if (CurrentUser != null)
                {
                    await LoadDashboardData();
                }

                IsLoading = false;
                Logger.LogInformation("HomePage initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing HomePage");
                IsLoading = false;
            }
        }

        #endregion


        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    if (firstRender)
        //    {
        //        // Blur any focused element on first render
        //        await Task.Delay(100);
        //        // This prevents the focus outline from showing
        //    }

        //    await base.OnAfterRenderAsync(firstRender);
        //}





        #region Data Loading Methods

       
        /// Load current user from authentication state
       
        private async Task LoadUserData()
        {
            try
            {
                Logger.LogInformation("LoadUserData: Starting...");

                // ✅ IMPORTANT: Await each operation completely before starting the next
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                Logger.LogInformation("LoadUserData: AuthState retrieved. IsAuthenticated: {IsAuthenticated}",
                    user?.Identity?.IsAuthenticated);

                if (user?.Identity?.IsAuthenticated != true)
                {
                    Logger.LogWarning("LoadUserData: User is not authenticated");
                    return;
                }

                var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                Logger.LogInformation("LoadUserData: Email from claims: {Email}", email ?? "NULL");

                if (string.IsNullOrEmpty(email))
                {
                    Logger.LogWarning("LoadUserData: Email claim not found");
                    return;
                }

                // ✅ IMPORTANT: Await this operation fully
                Logger.LogInformation("LoadUserData: Calling GetUserByEmailDapperAsync({Email})", email);
                //CurrentUser = await AuthService.GetUserByEmailAsync(email);       This is the original EF Core method, which cant work due to concurrency issues
                CurrentUser = await AuthService.GetUserByEmailDapperAsync(email);

                if (CurrentUser == null)
                {
                    Logger.LogError("LoadUserData: User not found in database for email: {Email}", email);
                    return;
                }

                CurrentBranch = CurrentUser.BranchId?.ToString() ?? "Default Branch";
                LastLoginDate = CurrentUser.LastLoginAt ?? DateTime.UtcNow.AddHours(-2);

                Logger.LogInformation(
                    "LoadUserData: Success - Email: {Email}, UserId: {UserId}, BranchId: {BranchId}",
                    email, CurrentUser.Id, CurrentUser.BranchId);
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogError(ex, "LoadUserData: InvalidOperationException - {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadUserData: Unexpected error - {ExceptionType}: {Message}",
                    ex.GetType().Name, ex.Message);
            }
        }


       
        /// Load all dashboard data for the current user
       
        private async Task LoadDashboardData()
        {
            try
            {
                Logger.LogInformation("LoadDashboardData: Starting...");

                if (CurrentUser == null)
                {
                    Logger.LogWarning("LoadDashboardData: CurrentUser is null");
                    return;
                }

                long userId = CurrentUser.Id;
                Logger.LogInformation("LoadDashboardData: Loading for userId: {UserId}", userId);

                try
                {
                    // ✅ IMPORTANT: Load these sequentially, not in parallel
                    // (if dashboard service uses the same DbContext)
                    Logger.LogInformation("LoadDashboardData: Fetching KPI metrics...");
                    KPIData = await DashboardDataService.GetKPIMetricsAsync(userId);

                    Logger.LogInformation("LoadDashboardData: Fetching recent activities...");
                    RecentActivities = await DashboardDataService.GetRecentActivitiesAsync(userId, 3);

                    Logger.LogInformation("LoadDashboardData: Fetching recent assessments...");
                    RecentAssessments = await DashboardDataService.GetRecentAssessmentsAsync(userId, 5);

                    Logger.LogInformation("LoadDashboardData: Fetching alerts...");
                    AlertsData = await DashboardDataService.GetAlertsAsync(userId);

                    Logger.LogInformation("LoadDashboardData: Fetching announcements...");
                    SystemAnnouncements = await DashboardDataService.GetSystemAnnouncementsAsync(userId);

                    Logger.LogInformation("LoadDashboardData: Fetching insights...");
                    InsightsData = await DashboardDataService.GetInsightsAsync(userId);

                    Logger.LogInformation("LoadDashboardData: Success for userId: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "LoadDashboardData: Error loading dashboard data - {ExceptionType}: {Message}",
                        ex.GetType().Name, ex.Message);

                    KPIData ??= new KPIMetricsModel();
                    RecentActivities ??= new List<ActivityLogModel>();
                    RecentAssessments ??= new List<AssessmentModel>();
                    AlertsData ??= new AlertsModel();
                    SystemAnnouncements ??= new List<SystemAnnouncementModel>();
                    InsightsData ??= new InsightsModel();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadDashboardData: Unexpected error - {ExceptionType}", ex.GetType().Name);
            }
        }


        /// Get initials from user's first and last name for avatar        
        /// <returns>Two-character initials or "?" if names missing</returns>
        private string GetInitials()
        {
            if (CurrentUser == null) return "?";
            var first = CurrentUser.FirstName?.FirstOrDefault() ?? '?';
            var last = CurrentUser.LastName?.FirstOrDefault() ?? '?';
            return $"{first}{last}".ToUpper();
        }


        /// Format the last login date using relative format for recent dates,
        /// absolute format for older dates (following dashboard specification)

        /// <param name="date">The last login date</param>
        /// <returns>Formatted date string</returns>
        private string FormatLastLoginDate(DateTime date)
        {
            var now = DateTime.UtcNow;
            var diff = now - date;

            // Less than 1 hour: show minutes
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} minutes ago";

            // Less than 24 hours: show hours
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours > 1 ? "s" : "")} ago";

            // Less than 1 week: show days
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} day{((int)diff.TotalDays > 1 ? "s" : "")} ago";

            // 1 week or more: show absolute date
            else
                return date.ToString("MMM d, h:mm tt");
        }


       
        /// Refresh dashboard data - called when user refreshes or filters change
       
        private async Task RefreshDashboard()
        {
            try
            {
                Logger.LogInformation("RefreshDashboard: Starting...");
                await LoadDashboardData();
                await InvokeAsync(StateHasChanged);
                Logger.LogInformation("RefreshDashboard: Complete");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RefreshDashboard: Error");
            }
        }

        #endregion

        #region Event Handlers

       
        /// Handle KPI card drill-down click
        /// Navigate to detailed view of specific KPI
       
        private void HandleKPIDrill(string kpiType)
        {
            Logger.LogInformation("HandleKPIDrill: {KPIType}", kpiType);

            switch (kpiType)
            {
                case "ActiveAssessments":
                    NavigationManager.NavigateTo("/assessments?status=InProgress");
                    break;
                case "CompletedAssessments":
                    NavigationManager.NavigateTo("/assessments?status=Completed");
                    break;
                case "PendingReviews":
                    NavigationManager.NavigateTo("/assessments?status=Pending");
                    break;
                case "YourWorkload":
                    NavigationManager.NavigateTo("/assessments?assigned=me");
                    break;
                case "TotalClientBase":
                    NavigationManager.NavigateTo("/businesses");
                    break;
                case "BranchAssessments":
                    NavigationManager.NavigateTo($"/assessments?branch={CurrentUser?.BranchId}");
                    break;
                default:
                    Logger.LogWarning("HandleKPIDrill: Unknown KPI type: {KPIType}", kpiType);
                    break;
            }
        }

       
        /// Handle quick action button click
        /// Action IDs: 1=NewAssessment, 2=NewBusiness, 3=NewClient, 4=ViewActivityLog
       
        private void HandleQuickAction(int actionId)
        {
            Logger.LogInformation("HandleQuickAction: {ActionId}", actionId);

            switch (actionId)
            {
                case 1:
                    NavigationManager.NavigateTo("/assessments/new");
                    break;
                case 2:
                    NavigationManager.NavigateTo("/businesses/new");
                    break;
                case 3:
                    NavigationManager.NavigateTo("/clients/new");
                    break;
                case 4:
                    NavigationManager.NavigateTo("/activity-log");
                    break;
                default:
                    Logger.LogWarning("HandleQuickAction: Unknown action ID: {ActionId}", actionId);
                    break;
            }
        }

       
        /// Handle recent activity filter refresh
       
        private async Task HandleRecentActivitiesRefresh()
        {
            try
            {
                Logger.LogInformation("HandleRecentActivitiesRefresh: Starting...");

                if (CurrentUser == null)
                {
                    Logger.LogWarning("HandleRecentActivitiesRefresh: CurrentUser is null");
                    return;
                }

                long userId = CurrentUser.Id;
                RecentActivities = await DashboardDataService.GetRecentActivitiesAsync(userId, 3);

                await InvokeAsync(StateHasChanged);
                Logger.LogInformation("HandleRecentActivitiesRefresh: Complete");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleRecentActivitiesRefresh: Error");
            }
        }

       
        /// Handle alert dismissal
       
        private async Task HandleAlertDismissal((string alertId, int actionTypeId) dismissalData)
        {
            try
            {
                var (alertId, actionTypeId) = dismissalData;

                var alertType = actionTypeId switch
                {
                    1 => "UrgentAssessment",
                    2 => "Announcement",
                    _ => "Unknown"
                };

                if (alertType == "Unknown")
                {
                    Logger.LogWarning("HandleAlertDismissal: Unknown action type: {ActionTypeId}", actionTypeId);
                    return;
                }

                Logger.LogInformation("HandleAlertDismissal: AlertId: {AlertId}, Type: {AlertType}, UserId: {UserId}",
                    alertId, alertType, CurrentUser?.Id);

                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleAlertDismissal: Error");
            }
        }

       
        /// Handle export data request
       
        private async Task HandleExport((int exportTypeId, int formatId) exportData)
        {
            try
            {
                var (exportTypeId, formatId) = exportData;

                var exportType = exportTypeId switch
                {
                    1 => "CompletionRate",
                    2 => "AvgCompletionTime",
                    3 => "StatusDistribution",
                    4 => "TopPerformers",
                    _ => "Unknown"
                };

                var format = formatId switch
                {
                    1 => "Excel",
                    2 => "PDF",
                    _ => "Unknown"
                };

                Logger.LogInformation("HandleExport: Type: {ExportType}, Format: {Format}, UserId: {UserId}",
                    exportType, format, CurrentUser?.Id);

                if (exportType == "Unknown" || format == "Unknown")
                {
                    Logger.LogWarning("HandleExport: Invalid parameters");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleExport: Error");
            }
        }

        #endregion
    }
}