using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents;

public partial class RecentActivitySnapshotComponent
{
    [Inject] private IActivityLogRepository ActivityRepository { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] private ILogger<RecentActivitySnapshotComponent> Logger { get; set; } = default!;

    // Kept temporarily so the existing HomePage invocation remains source-compatible.
    [Parameter] public List<ActivityLogModel>? Activities { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }

    private List<ActivityLogModel> LoadedActivities { get; set; } = new();
    private long CurrentUserId { get; set; }
    private string SelectedFilter { get; set; } = "24h";
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ResolveUserAndLoadAsync();
    }

    private async Task ResolveUserAndLoadAsync()
    {
        try
        {
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authenticationState.User.Identity?.IsAuthenticated != true)
            {
                ErrorMessage = "Sign in to view recent activity.";
                return;
            }

            var currentUser = await AuthenticationService.GetCurrentUserAsync(authenticationState.User);
            if (currentUser == null)
            {
                ErrorMessage = "Your user profile could not be resolved.";
                return;
            }

            CurrentUserId = currentUser.Id;
            await LoadActivitiesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize the recent activity snapshot");
            ErrorMessage = "Recent activity could not be loaded.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SelectFilterAsync(string filter)
    {
        SelectedFilter = filter;
        await LoadActivitiesAsync();
    }

    private Task Select24HoursAsync() => SelectFilterAsync("24h");
    private Task Select7DaysAsync() => SelectFilterAsync("7d");
    private Task Select30DaysAsync() => SelectFilterAsync("30d");
    private Task SelectAllAsync() => SelectFilterAsync("all");

    private async Task LoadActivitiesAsync()
    {
        if (CurrentUserId <= 0)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            LoadedActivities = await ActivityRepository.GetRecentActivitiesAsync(
                CurrentUserId,
                count: 5,
                filterType: SelectedFilter);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load recent activities for user {UserId}", CurrentUserId);
            ErrorMessage = "Recent activity could not be loaded.";
            LoadedActivities = new();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetFilterClass(string filter) =>
        SelectedFilter == filter ? "filter-option active" : "filter-option";

    private static string DisplayText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatActivityTime(DateTime createdDate)
    {
        var elapsed = DateTime.UtcNow - createdDate.ToUniversalTime();

        if (elapsed.TotalMinutes < 1) return "Just now";
        if (elapsed.TotalHours < 1) return $"{Math.Max(1, (int)elapsed.TotalMinutes)}m ago";
        if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 2) return "Yesterday";
        if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
        return createdDate.ToLocalTime().ToString("dd MMM, HH:mm");
    }

    private static string GetActivityActionClass(string? action) => action?.Trim().ToLowerInvariant() switch
    {
        "create" or "restore" or "login" => "action-success",
        "update" or "approve" or "submit" => "action-info",
        "delete" or "reject" or "logout" => "action-danger",
        "archive" or "import" => "action-warning",
        _ => "action-neutral"
    };


    private static string FormatAbbreviatedName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "System";
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // If only one name is provided, display it in full
        if (parts.Length == 1)
        {
            return parts[0];
        }

        // Abbreviates ONLY the first name to its initial, and keeps the last name in full
        // e.g., "John Doe" becomes "J. Doe"
        return $"{parts[0][0]}. {parts[^1]}";
    }


    private static string FormatEnumText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var result = new StringBuilder(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            if (index > 0 && char.IsUpper(value[index]) && char.IsLower(value[index - 1]))
            {
                result.Append(' ');
            }

            result.Append(value[index]);
        }

        return result.ToString();
    }
}
