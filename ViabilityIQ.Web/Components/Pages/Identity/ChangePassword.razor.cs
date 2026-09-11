using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ViabilityIQ.Application.Dtos.IdentityDtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.Identity;

public partial class ChangePassword : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public IPasswordService PasswordService { get; set; } = default!;
    [Inject] public IActivityLogWriter ActivityLogWriter { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public ILogger<ChangePassword> Logger { get; set; } = default!;

    public ChangePasswordRequest PasswordRequest { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;
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
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authenticationState.User.Identity?.IsAuthenticated != true)
            {
                ErrorMessage = "Please sign in before changing your password.";
                return;
            }

            var user = await AuthService.GetCurrentUserAsync(authenticationState.User);
            if (user == null)
            {
                ErrorMessage = "Your user profile could not be resolved.";
                return;
            }

            var result = await PasswordService.ChangePasswordAsync(
                user.Id,
                PasswordRequest.CurrentPassword,
                PasswordRequest.NewPassword);

            if (!result.Success)
            {
                ErrorMessage = string.Join(" ", result.Messages);
                return;
            }

            SuccessMessage = "Password updated successfully.";
            PasswordRequest = new ChangePasswordRequest();

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
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing password");
            ErrorMessage = "An unexpected error occurred while updating your password. Please try again.";
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
