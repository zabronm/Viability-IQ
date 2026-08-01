using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Dtos.IdentityDtos;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Application.ServicesMisc
{
    
    /// Password management service for handling password reset and email confirmation
    
    public class PasswordService : IPasswordService
    {
        #region Private Fields

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PasswordService> _logger;

        #endregion

        #region Constructor

        public PasswordService(
            UserManager<ApplicationUser> userManager,
            ILogger<PasswordService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Password Reset

        
        /// Initiates password reset process by generating a reset token
        
        public async Task<PasswordResetResult> GeneratePasswordResetTokenAsync(string email)
        {
            var result = new PasswordResetResult();

            try
            {
                _logger.LogInformation("Password reset requested for email: {Email}", email);

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.Success = false;
                    result.Messages.Add("Email address is required");
                    return result;
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // For security, don't reveal if user exists
                    result.Success = true;
                    result.Messages.Add("If an account exists with this email, you will receive a password reset link");
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                    return result;
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                result.Success = true;
                result.UserId = user.Id;
                result.ResetToken = token;
                result.Messages.Add("Password reset token generated successfully");

                _logger.LogInformation("Password reset token generated for user: {Email}", email);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error generating reset token: {ex.Message}");
                _logger.LogError(ex, "Exception generating password reset token for email: {Email}", email);
                return result;
            }
        }

        
        /// Resets user password using reset token
        
        public async Task<PasswordResetResult> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            var result = new PasswordResetResult();

            try
            {
                _logger.LogInformation("Password reset attempt for user ID: {UserId}", userId);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    result.Success = false;
                    result.Messages.Add("User ID is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    result.Success = false;
                    result.Messages.Add("Reset token is invalid or expired");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    result.Success = false;
                    result.Messages.Add("New password is required");
                    return result;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    result.Success = false;
                    result.Messages.Add("User not found");
                    _logger.LogWarning("Password reset failed: User not found for ID: {UserId}", userId);
                    return result;
                }

                // Reset password with token
                var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!resetResult.Succeeded)
                {
                    result.Success = false;
                    result.Messages = resetResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Password reset failed for user: {Email}. Errors: {Errors}",
                        user.Email, string.Join(", ", result.Messages));
                    return result;
                }

                result.Success = true;
                result.Messages.Add("Password has been reset successfully. Please log in with your new password.");

                _logger.LogInformation("Password reset successful for user: {Email}", user.Email);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error resetting password: {ex.Message}");
                _logger.LogError(ex, "Exception during password reset for user ID: {UserId}", userId);
                return result;
            }
        }

        #endregion

        #region Email Confirmation

        
        /// Generates email confirmation token
        
        public async Task<EmailConfirmationResult> GenerateEmailConfirmationTokenAsync(string userId)
        {
            var result = new EmailConfirmationResult();

            try
            {
                _logger.LogInformation("Email confirmation token requested for user ID: {UserId}", userId);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    result.Success = false;
                    result.Messages.Add("User ID is required");
                    return result;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    result.Success = false;
                    result.Messages.Add("User not found");
                    _logger.LogWarning("Email confirmation token failed: User not found for ID: {UserId}", userId);
                    return result;
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                result.Success = true;
                result.UserId = user.Id;
                result.Email = user.Email;
                result.ConfirmationToken = token;
                result.Messages.Add("Confirmation token generated successfully");

                _logger.LogInformation("Email confirmation token generated for user: {Email}", user.Email);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error generating confirmation token: {ex.Message}");
                _logger.LogError(ex, "Exception generating email confirmation token for user ID: {UserId}", userId);
                return result;
            }
        }

        
        /// Confirms user email address
        
        public async Task<EmailConfirmationResult> ConfirmEmailAsync(string userId, string token)
        {
            var result = new EmailConfirmationResult();

            try
            {
                _logger.LogInformation("Email confirmation attempt for user ID: {UserId}", userId);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    result.Success = false;
                    result.Messages.Add("User ID is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    result.Success = false;
                    result.Messages.Add("Confirmation token is invalid or expired");
                    return result;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    result.Success = false;
                    result.Messages.Add("User not found");
                    _logger.LogWarning("Email confirmation failed: User not found for ID: {UserId}", userId);
                    return result;
                }

                if (user.EmailConfirmed)
                {
                    result.Success = true;
                    result.Messages.Add("Email is already confirmed");
                    _logger.LogInformation("Email already confirmed for user: {Email}", user.Email);
                    return result;
                }

                var confirmResult = await _userManager.ConfirmEmailAsync(user, token);

                if (!confirmResult.Succeeded)
                {
                    result.Success = false;
                    result.Messages = confirmResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Email confirmation failed for user: {Email}. Errors: {Errors}",
                        user.Email, string.Join(", ", result.Messages));
                    return result;
                }

                result.Success = true;
                result.Email = user.Email;
                result.Messages.Add("Email confirmed successfully. You can now log in.");

                _logger.LogInformation("Email confirmed successfully for user: {Email}", user.Email);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error confirming email: {ex.Message}");
                _logger.LogError(ex, "Exception during email confirmation for user ID: {UserId}", userId);
                return result;
            }
        }

        #endregion

        #region Change Password

        
        /// Changes password for authenticated user
        
        public async Task<PasswordChangeResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var result = new PasswordChangeResult();

            try
            {
                _logger.LogInformation("Password change requested for user ID: {UserId}", userId);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    result.Success = false;
                    result.Messages.Add("User ID is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    result.Success = false;
                    result.Messages.Add("Current password is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    result.Success = false;
                    result.Messages.Add("New password is required");
                    return result;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    result.Success = false;
                    result.Messages.Add("User not found");
                    _logger.LogWarning("Password change failed: User not found for ID: {UserId}", userId);
                    return result;
                }

                var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

                if (!changeResult.Succeeded)
                {
                    result.Success = false;
                    result.Messages = changeResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Password change failed for user: {Email}", user.Email);
                    return result;
                }

                result.Success = true;
                result.Messages.Add("Password changed successfully");

                _logger.LogInformation("Password changed successfully for user: {Email}", user.Email);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error changing password: {ex.Message}");
                _logger.LogError(ex, "Exception during password change for user ID: {UserId}", userId);
                return result;
            }
        }

        #endregion
    }
}