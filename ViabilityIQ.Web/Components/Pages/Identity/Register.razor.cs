using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Register : ComponentBase
    {
        [Inject]
        public IAuthenticationService AuthService { get; set; } = default!;

        [Inject]
        public NavigationManager Navigation { get; set; } = default!;

        [Inject]
        public ILogger<Register> Logger { get; set; } = default!;

        public RegisterRequest RegisterRequest { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public bool ShowPassword { get; set; } = false;
        public bool ShowConfirmPassword { get; set; } = false;
        public bool AgreedToTerms { get; set; } = false;
        public bool IsSubmitting { get; set; } = false;

        private const int MinPasswordLength = 8;
        private const int SuccessRedirectDelayMs = 2000;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger.LogInformation("Register page initialized at {Timestamp}", DateTime.UtcNow);

                var isAuthenticated = await AuthService.IsUserAuthenticatedAsync();
                if (isAuthenticated)
                {
                    Logger.LogInformation("User already authenticated, redirecting to dashboard");
                    Navigation.NavigateTo("/dashboard", replace: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during register page initialization");
                ErrorMessage = "An error occurred while initializing the registration page.";
            }
        }

        public async Task HandleRegister()
        {
            if (IsSubmitting)
                return;

            IsSubmitting = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                Logger.LogInformation("Registration attempt initiated for email: {Email}", RegisterRequest.Email);

                var validationResult = ValidateRegistrationRequest();
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    Logger.LogWarning("Registration validation failed: {ValidationError}", validationResult.ErrorMessage);
                    IsSubmitting = false;
                    StateHasChanged();
                    return;
                }

                var result = await AuthService.RegisterAsync(RegisterRequest);

                if (result.Success)
                {
                    Logger.LogInformation(
                        "User {Email} registered successfully at {Timestamp}",
                        RegisterRequest.Email,
                        DateTime.UtcNow
                    );

                    SuccessMessage = result.Messages.FirstOrDefault()
                        ?? "Registration successful! Redirecting to login...";

                    ClearRegistrationForm();

                    await Task.Delay(SuccessRedirectDelayMs);
                    Navigation.NavigateTo("/login", replace: true);
                }
                else
                {
                    HandleRegistrationFailure(result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception occurred during registration for email: {Email}", RegisterRequest.Email);
                ErrorMessage = GetUserFriendlyErrorMessage(ex);
            }
            finally
            {
                IsSubmitting = false;
                StateHasChanged();
            }
        }

        public bool IsFormValid()
        {
            if (string.IsNullOrWhiteSpace(RegisterRequest.FirstName))
                return false;

            if (string.IsNullOrWhiteSpace(RegisterRequest.LastName))
                return false;

            if (string.IsNullOrWhiteSpace(RegisterRequest.Email))
                return false;

            if (string.IsNullOrWhiteSpace(RegisterRequest.Password))
                return false;

            if (string.IsNullOrWhiteSpace(RegisterRequest.ConfirmPassword))
                return false;

            if (RegisterRequest.Password != RegisterRequest.ConfirmPassword)
                return false;

            var pwdCheck = ValidatePasswordStrength(RegisterRequest.Password);
            if (!pwdCheck.IsValid)
                return false;

            if (!AgreedToTerms)
                return false;

            return true;
        }

        public string GetPasswordValidationFeedback(string password)
        {
            var result = ValidatePasswordStrength(password);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }

        public void TogglePasswordVisibility()
        {
            ShowPassword = !ShowPassword;
            StateHasChanged();
        }

        public void ToggleConfirmPasswordVisibility()
        {
            ShowConfirmPassword = !ShowConfirmPassword;
            StateHasChanged();
        }

        private RegistrationValidationResult ValidateRegistrationRequest()
        {
            var result = new RegistrationValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(RegisterRequest.FirstName) || RegisterRequest.FirstName.Length < 2)
            {
                result.IsValid = false;
                result.ErrorMessage = "First name must be at least 2 characters long.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(RegisterRequest.LastName) || RegisterRequest.LastName.Length < 2)
            {
                result.IsValid = false;
                result.ErrorMessage = "Last name must be at least 2 characters long.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(RegisterRequest.Email) || !IsValidEmail(RegisterRequest.Email))
            {
                result.IsValid = false;
                result.ErrorMessage = "Please enter a valid email address.";
                return result;
            }

            var passwordValidation = ValidatePasswordStrength(RegisterRequest.Password);
            if (!passwordValidation.IsValid)
            {
                result.IsValid = false;
                result.ErrorMessage = passwordValidation.ErrorMessage;
                return result;
            }

            if (RegisterRequest.Password != RegisterRequest.ConfirmPassword)
            {
                result.IsValid = false;
                result.ErrorMessage = "Passwords do not match.";
                return result;
            }

            if (!AgreedToTerms)
            {
                result.IsValid = false;
                result.ErrorMessage = "You must agree to the Terms of Service to continue.";
                return result;
            }

            return result;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private PasswordValidationResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Password must be at least {MinPasswordLength} characters long.";
                return result;
            }

            if (!password.Any(char.IsUpper))
            {
                result.IsValid = false;
                result.ErrorMessage = "Password must contain at least one uppercase letter.";
                return result;
            }

            if (!password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.ErrorMessage = "Password must contain at least one lowercase letter.";
                return result;
            }

            if (!password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.ErrorMessage = "Password must contain at least one digit.";
                return result;
            }

            if (!HasSpecialCharacter(password))
            {
                result.IsValid = false;
                result.ErrorMessage = "Password must contain at least one special character (!@#$%^&*).";
                return result;
            }

            return result;
        }

        private bool HasSpecialCharacter(string password)
        {
            const string specialCharacters = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            return password.Any(c => specialCharacters.Contains(c));
        }

        private void HandleRegistrationFailure(AuthResult result)
        {
            if (result.Messages == null || result.Messages.Count == 0)
            {
                ErrorMessage = "An unknown error occurred during registration. Please try again.";
                return;
            }

            var errorMessages = string.Join(" ", result.Messages);

            if (errorMessages.Contains("email", StringComparison.OrdinalIgnoreCase) &&
                errorMessages.Contains("registered", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "This email address is already registered. Please use a different email or try logging in.";
            }
            else
            {
                ErrorMessage = errorMessages;
            }
        }

        private string GetUserFriendlyErrorMessage(Exception ex)
        {
            return ex switch
            {
                ArgumentNullException => "All required fields must be filled out.",
                ArgumentException => "Invalid input format. Please check your information.",
                InvalidOperationException => "A system error occurred. Please contact support.",
                TimeoutException => "The registration service is temporarily unavailable. Please try again.",
                HttpRequestException => "Network error occurred. Please check your connection and try again.",
                _ => "An unexpected error occurred. Please try again later."
            };
        }

        private void ClearRegistrationForm()
        {
            RegisterRequest = new();
            ShowPassword = false;
            ShowConfirmPassword = false;
            AgreedToTerms = false;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        private class RegistrationValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class PasswordValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}