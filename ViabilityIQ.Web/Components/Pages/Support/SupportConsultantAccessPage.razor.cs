using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.Support;

public partial class SupportConsultantAccessPage
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ILogger<SupportConsultantAccessPage> Logger { get; set; } = default!;

    private List<SupportUserRow> _users = [];
    private string? _message;
    private long? _updatingUserId;
    private bool _isLoading = true;
    private bool _isError;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to load support consultant access list.");
            _isError = true;
            _message = "The support access list could not be loaded.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadUsersAsync()
    {
        var users = await UserManager.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToListAsync();
        var consultants = (await UserManager.GetUsersInRoleAsync(SupportRoleSeeder.RoleName))
            .Select(user => user.Id)
            .ToHashSet();

        _users = users.Select(user => new SupportUserRow
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            IsConsultant = consultants.Contains(user.Id)
        }).ToList();
    }

    private async Task ToggleConsultantAsync(SupportUserRow user)
    {
        _updatingUserId = user.Id;
        _message = null;
        _isError = false;
        try
        {
            var applicationUser = await UserManager.FindByIdAsync(user.Id.ToString());
            if (applicationUser is null)
                throw new InvalidOperationException("The selected staff account no longer exists.");

            var result = user.IsConsultant
                ? await UserManager.RemoveFromRoleAsync(applicationUser, SupportRoleSeeder.RoleName)
                : await UserManager.AddToRoleAsync(applicationUser, SupportRoleSeeder.RoleName);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));

            _message = user.IsConsultant
                ? $"{user.FirstName} {user.LastName} no longer has support inbox access."
                : $"{user.FirstName} {user.LastName} now has support inbox access. They need to sign in again for the role to appear in their session.";
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to update support consultant access for user {UserId}.", user.Id);
            _isError = true;
            _message = "Support consultant access could not be updated.";
        }
        finally
        {
            _updatingUserId = null;
        }
    }

    private sealed class SupportUserRow
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsConsultant { get; init; }
    }
}
