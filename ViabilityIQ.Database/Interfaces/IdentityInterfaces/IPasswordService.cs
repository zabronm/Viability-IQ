using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Dtos.IdentityDtos;

namespace ViabilityIQ.Application.Interfaces.IdentityInterfaces
{
    
    /// Interface for password management operations
    
    public interface IPasswordService
    {
        
        /// Generates a password reset token for the user        
        Task<PasswordResetResult> GeneratePasswordResetTokenAsync(string email);

        
        /// Resets the user's password using a valid reset token        
        Task<PasswordResetResult> ResetPasswordAsync(string userId, string token, string newPassword);

        
        /// Generates an email confirmation token        
        Task<EmailConfirmationResult> GenerateEmailConfirmationTokenAsync(string userId);

        
        /// Confirms the user's email address        
        Task<EmailConfirmationResult> ConfirmEmailAsync(string userId, string token);

        
        /// Changes the password for an authenticated user        
        Task<PasswordChangeResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    }
}