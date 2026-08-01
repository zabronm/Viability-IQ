using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.ComponentModel.DataAnnotations;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Web.Extensions;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    
    /// Login page component for user authentication
    /// Handles user login with email and password credentials
    
    public partial class Login : ComponentBase
    {
        #region Injected Dependencies

        [Inject] public ToastService _Toast { get; set; }
        [Inject]        public CustomAuthenticationStateProvider CustomAuthProvider { get; set; } = default!;
        [Inject]        public IAuthenticationService AuthService { get; set; } = default!;   
        [Inject]        public NavigationManager Navigation { get; set; } = default!; 
        [Inject]        public ILogger<Login> Logger { get; set; } = default!;
        [Inject]        public ToastService ToastService { get; set; } = default!;

        #endregion

        #region Form State Properties                   
        
        private LoginRequest LoginRequest = new();
        private string ErrorMessage = string.Empty;
        private bool showPassword = false;        
        private bool isSubmitting = false;

        #endregion
        #region Constants
                
       
        private const int MaxLoginAttempts = 5;
        private const int LockoutDurationMinutes = 5;

        #endregion
        #region Lifecycle Methods

        
        /// Called after the component is initialized
        
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger.LogInformation("Login page initialized at {Timestamp}", DateTime.UtcNow);

                // Check if user is already authenticated
                var isAuthenticated = await AuthService.IsUserAuthenticatedAsync();
                if (isAuthenticated)
                {
                    Logger.LogInformation("User already authenticated, redirecting to dashboard");
                    Navigation.NavigateTo("/dashboard", replace: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during login page initialization");
                ErrorMessage = "An error occurred while initializing the login page.";
                ToastService.ShowError(ErrorMessage, "Initialization Error");
            }
        }

        #endregion

        #region Form Submission

        
        /// Handles the login form submission
        /// Validates credentials and authenticates the user
        
        private async Task HandleLogin()
        {
            if (isSubmitting) return;
            isSubmitting = true;
            ErrorMessage = string.Empty;

            try
            {            

                // Step 1: Validate             
                var validationResult = ValidateLoginRequest();
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    isSubmitting = false;
                    return;
                }                               

                // Step 2: Call auth service
                var result = await AuthService.LoginAsync(LoginRequest);              
                if (result.Success)
                {
                    try
                    {
                        Logger.LogInformation("Login successful, notifying auth state");

                        // Get the authenticated user
                        var authenticatedUser = await AuthService.GetUserByEmailAsync(LoginRequest.Email);

                        if (authenticatedUser != null)
                        {
                            // Notify the provider that user is authenticated
                            // This is REQUIRED for Remember Me to work
                            await CustomAuthProvider.NotifyUserAuthenticationAsync(authenticatedUser);
                            Logger.LogInformation("User authentication state notified successfully");
                        }

                        ToastService.ShowSuccess($"Welcome back, {result.FirstName}!", "Login Successful");
                        ClearLoginForm();

                        await Task.Delay(500);
                        Navigation.NavigateTo("/home", replace: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error during authentication state notification");
                        ErrorMessage = "Login succeeded but an error occurred. Please try again.";
                        ToastService.ShowError(ErrorMessage, "Login Error");
                    }

                }
                else
                {                   
                    HandleAuthenticationFailure(result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"🔍 DEBUG: Exception in HandleLogin: {ex.Message}");
                ErrorMessage = ex.Message;
            }
            finally
            {
                isSubmitting = false;
            }
        }

        #endregion

        #region Validation Methods

        
        /// Checks if the form is valid and ready to submit
        
        private bool IsFormValid()
        {
            return !string.IsNullOrWhiteSpace(LoginRequest.Email) &&
                   !string.IsNullOrWhiteSpace(LoginRequest.Password) &&
                   LoginRequest.Email.Length > 0 &&
                   LoginRequest.Password.Length >= 8;
        }

        
        /// Validates the login request
        
        /// <returns>Validation result with error message if validation fails</returns>
        private LoginValidationResult ValidateLoginRequest()
        {
            var result = new LoginValidationResult { IsValid = true };

            // Check email is provided
            if (string.IsNullOrWhiteSpace(LoginRequest.Email))
            {
                result.IsValid = false;
                result.ErrorMessage = "Email address is required.";
                return result;
            }

            // Check email format
            if (!IsValidEmail(LoginRequest.Email))
            {
                result.IsValid = false;
                result.ErrorMessage = "Please enter a valid email address.";
                return result;
            }

            // Check password is provided
            if (string.IsNullOrWhiteSpace(LoginRequest.Password))
            {
                result.IsValid = false;
                result.ErrorMessage = "Password is required.";
                return result;
            }

            // Check password minimum length
            if (LoginRequest.Password.Length < 8)
            {
                result.IsValid = false;
                result.ErrorMessage = "Password must be at least 8 characters long.";
                return result;
            }

            return result;
        }

        
        /// Validates email format using MailAddress
        
        /// <param name="email">Email address to validate</param>
        /// <returns>True if email is valid, false otherwise</returns>
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

        #endregion

        #region Error Handling

        
        /// Handles authentication service failures
        /// Provides user-friendly error messages based on failure type
        
        /// <param name="result">Authentication result from service</param>
        private void HandleAuthenticationFailure(AuthResult result)
        {
            if (result.Messages == null || result.Messages.Count == 0)
            {
                ErrorMessage = "An unknown error occurred during login. Please try again.";
                ToastService.ShowError(ErrorMessage, "Authentication Error");
                return;
            }

            // Combine all error messages
            var errorMessages = string.Join(" ", result.Messages);

            // Check for specific error conditions
            if (errorMessages.Contains("locked", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = $"{errorMessages} Please try again in {LockoutDurationMinutes} minutes or reset your password.";
                ToastService.ShowError(ErrorMessage, "Account Locked");
                Logger.LogWarning("Account locked for email: {Email}", LoginRequest.Email);
            }
            else if (errorMessages.Contains("confirmation", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = $"{errorMessages} Check your email for the confirmation link.";
                ToastService.ShowWarning(ErrorMessage, "Email Confirmation Required");
                Logger.LogWarning("Email confirmation required for email: {Email}", LoginRequest.Email);
            }
            else if (errorMessages.Contains("inactive", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Your account has been deactivated. Please contact system administrator.";
                ToastService.ShowError(ErrorMessage, "Account Inactive");
                Logger.LogWarning("Inactive account login attempt: {Email}", LoginRequest.Email);
            }
            else if (errorMessages.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Invalid email or password. Please try again.";
                ToastService.ShowError(ErrorMessage, "Invalid Credentials");
                Logger.LogWarning("Invalid password attempt for email: {Email}", LoginRequest.Email);
            }
            else
            {
                ErrorMessage = errorMessages;
                ToastService.ShowError(errorMessages, "Login Failed");
            }
        }

        
        /// Converts exceptions to user-friendly error messages
        
        /// <param name="ex">Exception that occurred</param>
        /// <returns>User-friendly error message</returns>
        private string GetUserFriendlyErrorMessage(Exception ex)
        {
            return ex switch
            {
                ArgumentNullException => "Email and password are required.",
                ArgumentException => "Invalid email or password format.",
                InvalidOperationException => "A system error occurred. Please contact support.",
                TimeoutException => "The login service is temporarily unavailable. Please try again.",
                HttpRequestException => "Network error occurred. Please check your connection and try again.",
                _ => "An unexpected error occurred. Please try again later."
            };
        }

        #endregion

        #region UI Interaction Methods

        
        /// Toggles the password field visibility
        /// Shows/hides the password text
        
        private void TogglePasswordVisibility()
        {
            showPassword = !showPassword;
            Logger.LogDebug("Password visibility toggled. Currently visible: {IsVisible}", showPassword);
            StateHasChanged(); // Force component re-render
        }

        
        /// Clears the login form
        /// Called after successful login or on demand
        
        private void ClearLoginForm()
        {
            LoginRequest = new();
            showPassword = false;
            ErrorMessage = string.Empty;
        }

        #endregion

        #region Helper Classes

        
        /// Represents the result of client-side validation
        
        private class LoginValidationResult
        {
            
            /// Indicates whether validation passed
            
            public bool IsValid { get; set; }

            
            /// Error message if validation failed
            
            public string ErrorMessage { get; set; } = string.Empty;
        }

        #endregion
    }
}