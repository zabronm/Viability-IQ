using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Extensions
{
    
    /// Custom Authentication State Provider for Blazor Server
    /// Integrates with ASP.NET Core Identity to manage user authentication state
    
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        #region Private Fields

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;

        #endregion

        #region Constructor

        
        /// Initializes a new instance of the CustomAuthenticationStateProvider
        
        public CustomAuthenticationStateProvider(
            UserManager<ApplicationUser> userManager,
            ILogger<CustomAuthenticationStateProvider> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        
        /// Gets the current authentication state
        /// Called by Blazor to determine if user is authenticated
        
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                _logger.LogDebug("GetAuthenticationStateAsync called");

                // Create anonymous principal by default
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }
        }

        
        /// Notifies authentication state has changed
        /// Call this after successful login to refresh authentication state
        
        public async Task NotifyUserAuthenticationAsync(ApplicationUser user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("NotifyUserAuthenticationAsync called with null user");
                    var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
                    return;
                }

                _logger.LogInformation("NotifyUserAuthenticationAsync: User {Email} authenticated", user.Email);

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogDebug("User roles: {Roles}", string.Join(", ", roles));

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim("FirstName", user.FirstName ?? ""),
                    new Claim("LastName", user.LastName ?? ""),
                };

                // Add role claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Create authenticated identity
                var identity = new ClaimsIdentity(claims, "Custom");
                var principal = new ClaimsPrincipal(identity);

                // Notify Blazor that authentication state has changed
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));

                _logger.LogInformation("User authentication state notified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user authentication");
                throw;
            }
        }

        
        /// Notifies user has logged out
        
        public void NotifyUserLogout()
        {
            try
            {
                _logger.LogInformation("NotifyUserLogout called");

                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));

                _logger.LogInformation("User logout notified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user logout");
                throw;
            }
        }

        #endregion
    }
}