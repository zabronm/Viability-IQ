using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos.IdentityDtos;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;


namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class ChangePassword : ComponentBase
    {
        [Inject] public IAuthenticationService AuthService { get; set; } = default!;
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public ILogger<ChangePassword> Logger { get; set; } = default!;

        public ChangePasswordRequest PasswordRequest { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public bool ShowCurrentPassword { get; set; } = false;
        public bool ShowNewPassword { get; set; } = false;
        public bool ShowConfirmPassword { get; set; } = false;
        public bool IsSubmitting { get; set; } = false;

        public async Task HandlePasswordChange()
        {
            if (IsSubmitting) return;

            IsSubmitting = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                Logger.LogInformation("Processing password change request");

                // Replace with actual authentication service password change call
                await Task.Delay(800);

                SuccessMessage = "Password updated successfully!";
                PasswordRequest = new(); // Reset form fields
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

        public bool IsFormValid()
        {
            return !string.IsNullOrWhiteSpace(PasswordRequest.CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(PasswordRequest.NewPassword) &&
                   !string.IsNullOrWhiteSpace(PasswordRequest.ConfirmNewPassword) &&
                   PasswordRequest.NewPassword == PasswordRequest.ConfirmNewPassword &&
                   PasswordRequest.NewPassword.Length >= 6;
        }

        public void ToggleCurrentPasswordVisibility() => ShowCurrentPassword = !ShowCurrentPassword;
        public void ToggleNewPasswordVisibility() => ShowNewPassword = !ShowNewPassword;
        public void ToggleConfirmPasswordVisibility() => ShowConfirmPassword = !ShowConfirmPassword;
    }
}

