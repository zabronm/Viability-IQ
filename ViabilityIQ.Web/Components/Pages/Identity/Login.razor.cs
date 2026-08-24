using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Login : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public ILogger<Login> Logger { get; set; } = default!;

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