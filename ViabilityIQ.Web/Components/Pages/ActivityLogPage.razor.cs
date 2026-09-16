using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels.HomePageModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages;

public partial class ActivityLogPage
{
    [Inject] private IActivityLogRepository ActivityRepository { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<ActivityLogPage> Logger { get; set; } = default!;

    private static readonly string[] ActionOptions = Enum.GetNames<ActivityAction>();
    private static readonly string[] EntityTypeOptions = Enum.GetNames<ActivityEntityType>();

    private ActivityLogPageResult Result { get; set; } = new();
    private long CurrentUserId { get; set; }
    private string SearchText { get; set; } = string.Empty;
    private string SelectedAction { get; set; } = string.Empty;
    private string SelectedEntityType { get; set; } = string.Empty;
    private DateTime? FromDate { get; set; }
    private DateTime? ToDate { get; set; }
    private long? ExpandedActivityId { get; set; }
    private int PageNumber { get; set; } = 1;
    private int PageSize { get; set; } = 20;
    private bool IsLoading { get; set; }
    private string? ErrorMessage { get; set; }

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(Result.FilteredCount / (double)PageSize));
    private int FirstVisibleRecord => Result.FilteredCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    private int LastVisibleRecord => Math.Min(PageNumber * PageSize, Result.FilteredCount);

    protected override async Task OnInitializedAsync()
    {
        await ResolveAuthenticatedUserAsync();

        if (CurrentUserId > 0)
        {
            await LoadActivitiesAsync();
        }
    }

    private async Task ResolveAuthenticatedUserAsync()
    {
        try
        {
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authenticationState.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                ErrorMessage = "Sign in to view your activity history.";
                return;
            }

            var applicationUser = await AuthenticationService.GetCurrentUserAsync(user);
            if (applicationUser == null)
            {
                ErrorMessage = "Your user profile could not be resolved.";
                return;
            }

            CurrentUserId = applicationUser.Id;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resolve the authenticated user for the activity log");
            ErrorMessage = "Your user context could not be loaded.";
        }
    }

    private async Task LoadActivitiesAsync()
    {
        if (CurrentUserId <= 0)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        ExpandedActivityId = null;

        try
        {
            Result = await ActivityRepository.GetActivityPageAsync(new ActivityLogQueryModel
            {
                UserId = CurrentUserId,
                SearchText = SearchText,
                ActivityAction = SelectedAction,
                EntityType = SelectedEntityType,
                FromUtc = ToUtc(FromDate),
                ToUtcExclusive = ToDate.HasValue ? ToUtc(ToDate.Value.AddDays(1)) : null,
                PageNumber = PageNumber,
                PageSize = PageSize
            });

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
                Result = await ActivityRepository.GetActivityPageAsync(new ActivityLogQueryModel
                {
                    UserId = CurrentUserId,
                    SearchText = SearchText,
                    ActivityAction = SelectedAction,
                    EntityType = SelectedEntityType,
                    FromUtc = ToUtc(FromDate),
                    ToUtcExclusive = ToDate.HasValue ? ToUtc(ToDate.Value.AddDays(1)) : null,
                    PageNumber = PageNumber,
                    PageSize = PageSize
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load activity history for user {UserId}", CurrentUserId);
            ErrorMessage = "Activity history could not be loaded. Confirm that the activity-log table and repository schema are aligned.";
            Result = new ActivityLogPageResult();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date > ToDate.Value.Date)
        {
            ErrorMessage = "The From date cannot be later than the To date.";
            return;
        }

        PageNumber = 1;
        await LoadActivitiesAsync();
    }

    private async Task ResetFiltersAsync()
    {
        SearchText = string.Empty;
        SelectedAction = string.Empty;
        SelectedEntityType = string.Empty;
        FromDate = null;
        ToDate = null;
        PageNumber = 1;
        await LoadActivitiesAsync();
    }

    private async Task HandleSearchKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await ApplyFiltersAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (PageNumber <= 1)
        {
            return;
        }

        PageNumber--;
        await LoadActivitiesAsync();
    }

    private async Task NextPageAsync()
    {
        if (PageNumber >= TotalPages)
        {
            return;
        }

        PageNumber++;
        await LoadActivitiesAsync();
    }

    private async Task ChangePageSizeAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var pageSize))
        {
            return;
        }

        PageSize = pageSize;
        PageNumber = 1;
        await LoadActivitiesAsync();
    }

    private void ToggleDetails(long activityId) =>
        ExpandedActivityId = ExpandedActivityId == activityId ? null : activityId;

    private void OpenRelatedActivity(string? navigationUrl)
    {
        if (!string.IsNullOrWhiteSpace(navigationUrl))
        {
            Navigation.NavigateTo(navigationUrl);
        }
    }

    private async Task PrintPageAsync() =>
        await JS.InvokeVoidAsync("print");

    private async Task ExportCurrentPageAsync()
    {
        if (Result.Items.Count == 0)
        {
            return;
        }

        var csv = new StringBuilder();
        csv.AppendLine("Date (UTC),Actor,Action,Entity Type,Entity Name,Assessment,Module,Page,Remarks");

        foreach (var activity in Result.Items)
        {
            csv.AppendLine(string.Join(",",
                Csv(activity.CreatedDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(activity.ActorName),
                Csv(activity.ActivityAction),
                Csv(activity.EntityType),
                Csv(activity.EntityName),
                Csv(activity.AssessmentName),
                Csv(activity.Module),
                Csv(activity.Page),
                Csv(activity.Remarks)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var fileName = $"Activity_Log_Page_{PageNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        await JS.InvokeVoidAsync(
            "ZabFileSaver.DownloadBinaryStream",
            fileName,
            Convert.ToBase64String(bytes));
    }

    private static string Csv(string? value) =>
        $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private static DateTime? ToUtc(DateTime? localDate) =>
        localDate.HasValue
            ? DateTime.SpecifyKind(localDate.Value, DateTimeKind.Local).ToUniversalTime()
            : null;

    private static string DisplayText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatEntityId(long? entityId) =>
        entityId.HasValue ? $"#{entityId.Value}" : string.Empty;

    private static string FormatAction(string? action) =>
        string.IsNullOrWhiteSpace(action) ? "Unknown" : FormatEnumText(action);

    private static string FormatEnumText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
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

    private static string GetActionClass(string? action) => action?.Trim().ToLowerInvariant() switch
    {
        "create" or "restore" or "login" => "action-success",
        "update" or "approve" or "submit" => "action-info",
        "view" or "print" or "export" or "share" => "action-neutral",
        "delete" or "reject" or "logout" => "action-danger",
        "archive" or "import" => "action-warning",
        _ => "action-default"
    };
}
