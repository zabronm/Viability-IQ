using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;


namespace ViabilityIQ.Application.Interfaces.IdentityInterfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task LogoutAsync(ClaimsPrincipal user);
        Task<bool> IsUserAuthenticatedAsync();
        Task<ApplicationUser> GetCurrentUserAsync(ClaimsPrincipal user);
        Task<string> GetUserClaimAsync(string claimType);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> GetUserByEmailDapperAsync(string email);  //This uses Dapper to get the user by email, which is faster than EF Core for this specific query
        /// <summary>
        /// Get user by ID (long)
        /// </summary>
        Task<ApplicationUser> GetUserByIdAsync(long userId);
        Task<bool> HasRoleAsync(ClaimsPrincipal user, string roleName);

    }
}
