using Microsoft.AspNetCore.Components;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Login : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public ILogger<Login> Logger { get; set; } = default!;
        [Inject] public ToastService ToastService { get; set; } = default!;

        private string? ErrorMessage;
        private bool showPassword = false;
        private bool isLoading = false;

        protected override void OnInitialized()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue("error", out var errorVal))
            {
                ErrorMessage = errorVal.ToString() switch
                {
                    "InvalidCredentials" => "Invalid email or password. Please try again.",
                    "FieldsAreRequired" => "Please fill in all required fields.",
                    _ => "Authentication failed. Please try again."
                };
            }

            if (queryParams.TryGetValue("reason", out var reason)
                && reason.ToString() == "SessionExpired")
            {
                ToastService.ShowWarning(
                    "You were signed out after 30 minutes of inactivity. Please sign in to continue.",
                    "Session Expired");
            }
        }

        private void TogglePasswordVisibility()
        {
            showPassword = !showPassword;
            StateHasChanged();
        }

        private void HandleSubmit()
        {
            isLoading = true;
        }
    }
}