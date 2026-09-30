using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ViabilityIQ.Application.Dtos.IdentityDtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.Identity;

public partial class ChangePassword : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public IPasswordService PasswordService { get; set; } = default!;
    [Inject] public IActivityLogWriter ActivityLogWriter { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public ILogger<ChangePassword> Logger { get; set; } = default!;
    [Inject] public UserManager<ViabilityIQ.Shared.DataModels.SecurityDataModels.ApplicationUser> UserManager { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    public ChangePasswordRequest PasswordRequest { get; set; } = new();
    public bool ShowCurrentPassword { get; set; }
    public bool ShowNewPassword { get; set; }
    public bool ShowConfirmPassword { get; set; }
    public bool IsSubmitting { get; set; }

    public async Task HandlePasswordChange()
    {
        if (IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;

        try
        {
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authenticationState.User.Identity?.IsAuthenticated != true)
            {
                ToastService.ShowWarning("Please sign in before changing your password.", "Authentication Required");
                return;
            }

            var user = await AuthService.GetCurrentUserAsync(authenticationState.User);
            if (user == null)
            {
                ToastService.ShowError("Your user profile could not be resolved.", "Profile Unavailable");
                return;
            }

            var result = await PasswordService.ChangePasswordAsync(
                user.Id,
                PasswordRequest.CurrentPassword,
                PasswordRequest.NewPassword);

            if (!result.Success)
            {
                ToastService.ShowError(
                    result.Messages.Any()
                        ? string.Join(" ", result.Messages)
                        : "The password could not be updated.",
                    "Update Failed");
                return;
            }

            PasswordRequest = new ChangePasswordRequest();

            var identityUser = await UserManager.FindByIdAsync(user.Id.ToString());
            if (identityUser is not null && identityUser.MustChangePassword)
            {
                identityUser.MustChangePassword = false;
                var updateResult = await UserManager.UpdateAsync(identityUser);
                if (!updateResult.Succeeded)
                {
                    Logger.LogError(
                        "Password changed for user {UserId}, but MustChangePassword could not be cleared: {Errors}",
                        user.Id,
                        string.Join("; ", updateResult.Errors.Select(error => error.Description)));
                    ToastService.ShowError(
                        "The password changed, but the temporary-password restriction could not be cleared. Contact support.",
                        "Account Update Incomplete");
                    return;
                }
            }

            try
            {
                await ActivityLogWriter.RecordAsync(new ActivityLogWriteRequest
                {
                    Action = ActivityAction.Update,
                    EntityType = nameof(ActivityEntityType.User),
                    EntityId = user.Id,
                    EntityName = user.Email,
                    UserId = user.Id,
                    ActorName = $"{user.FirstName} {user.LastName}".Trim(),
                    Module = "Identity",
                    Page = "/identity/change-password",
                    Remarks = "User changed their password."
                });
            }
            catch (Exception auditException)
            {
                Logger.LogError(
                    auditException,
                    "Password changed for user {UserId}, but activity recording failed",
                    user.Id);
            }

            ToastService.ShowSuccess("Password updated successfully.", "Password Updated");
            await Task.Delay(750);
            Navigation.NavigateTo("/home", replace: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing password");
            ToastService.ShowError(
                "An unexpected error occurred while updating your password. Please try again.",
                "Update Failed");
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    public bool IsFormValid() =>
        !string.IsNullOrWhiteSpace(PasswordRequest.CurrentPassword)
        && !string.IsNullOrWhiteSpace(PasswordRequest.NewPassword)
        && !string.IsNullOrWhiteSpace(PasswordRequest.ConfirmNewPassword)
        && PasswordRequest.NewPassword == PasswordRequest.ConfirmNewPassword
        && PasswordRequest.NewPassword.Length >= 6;

    public void ToggleCurrentPasswordVisibility() => ShowCurrentPassword = !ShowCurrentPassword;
    public void ToggleNewPasswordVisibility() => ShowNewPassword = !ShowNewPassword;
    public void ToggleConfirmPasswordVisibility() => ShowConfirmPassword = !ShowConfirmPassword;
}
