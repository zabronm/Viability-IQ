using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;



namespace ViabilityIQ.Web.Components.Pages
{

    public partial class LogUserPage
    {
        [Inject] NavigationManager? Navigate { get; set; }
        private LoginViewModel loginModel = new LoginViewModel();
        private bool showPassword = false;
        private bool isSubmitting = false;

        private void TogglePasswordVisibility() => showPassword = !showPassword;

        private async Task HandleLogin()
        {
            isSubmitting = true;

            // Place identity verification handler / service logic layers here
            //await Task.Delay(1500);
            //isSubmitting = false;
            Navigate!.NavigateTo("/home");            
        }

        private void NavigateToForgotPassword()
        {
            // Integration context navigation route
        }

        private void NavigateToProduct()
        {
            var str_url = "http://www.ndsolutions.co.za";
            Navigate!.NavigateTo("/home");
        }

        private void NavigateToResetPassword()
        {
            // Integration context navigation route
        }

        private void NavigateToSignUp()
        {
            Navigate.NavigateTo("/home");
        }

        /* Clean Internal DTO Validation Schema */
        public class LoginViewModel
        {
            [Required(ErrorMessage = "Email credentials are required.")]
            [EmailAddress(ErrorMessage = "Please enter a valid business email address.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password configuration cannot be blank.")]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }
    }
}
