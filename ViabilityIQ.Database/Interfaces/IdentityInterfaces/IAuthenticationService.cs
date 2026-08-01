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
        Task<bool> HasRoleAsync(ClaimsPrincipal user, string roleName);


    }
}
