using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Extensions
{
    /// <summary>
    /// Custom Authentication State Provider for Blazor Server
    /// Integrates with ASP.NET Core Identity to read the actual authentication state from HttpContext
    /// </summary>
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        #region Private Fields

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Constructor

        public CustomAuthenticationStateProvider(
            UserManager<ApplicationUser> userManager,
            ILogger<CustomAuthenticationStateProvider> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current authentication state from HttpContext
        /// This reads from ASP.NET Core's built-in authentication system
        /// </summary>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                if (httpContext == null)
                {
                    _logger.LogWarning("[CustomAuthenticationStateProvider] HttpContext is null");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                _logger.LogDebug("[CustomAuthenticationStateProvider] GetAuthenticationStateAsync called");

                // ✅ READ FROM HTTPCONTENT USER (THIS IS THE ACTUAL AUTHENTICATED USER)
                var user = httpContext.User;

                _logger.LogInformation("[CustomAuthenticationStateProvider] User.Identity.IsAuthenticated: {IsAuthenticated}", user?.Identity?.IsAuthenticated);
                _logger.LogInformation("[CustomAuthenticationStateProvider] User.Identity.AuthenticationType: {AuthType}", user?.Identity?.AuthenticationType);
                _logger.LogInformation("[CustomAuthenticationStateProvider] Claims count: {ClaimCount}", user?.Claims.Count() ?? 0);

                // List all claims for debugging
                if (user?.Claims.Any() == true)
                {
                    foreach (var claim in user.Claims.Take(10)) // First 10 claims
                    {
                        _logger.LogDebug("[CustomAuthenticationStateProvider] Claim - {Type}: {Value}", claim.Type, claim.Value);
                    }
                }

                if (user?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("[CustomAuthenticationStateProvider] ✓ User IS authenticated from HttpContext");

                    // Get email from claims
                    var email = user.FindFirst(ClaimTypes.Email)?.Value;
                    _logger.LogInformation("[CustomAuthenticationStateProvider] User email: {Email}", email);

                    // Return the actual user principal from HttpContext
                    return new AuthenticationState(user);
                }
                else
                {
                    _logger.LogDebug("[CustomAuthenticationStateProvider] User is NOT authenticated, returning anonymous");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CustomAuthenticationStateProvider] Error getting authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        /// <summary>
        /// Notifies authentication state has changed
        /// Call this after successful login to refresh authentication state
        /// </summary>
        public async Task NotifyUserAuthenticationAsync(ApplicationUser user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("[CustomAuthenticationStateProvider] NotifyUserAuthenticationAsync called with null user");
                    return;
                }

                _logger.LogInformation("[CustomAuthenticationStateProvider] NotifyUserAuthenticationAsync: User {Email} - Triggering auth state refresh", user.Email);

                // ✅ Trigger re-evaluation of auth state
                // This will cause GetAuthenticationStateAsync to be called again
                var authState = await GetAuthenticationStateAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                _logger.LogInformation("[CustomAuthenticationStateProvider] ✓ Authentication state refresh notified");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CustomAuthenticationStateProvider] Error notifying user authentication");
                throw;
            }
        }

        /// <summary>
        /// Notifies user has logged out
        /// </summary>
        public void NotifyUserLogout()
        {
            try
            {
                _logger.LogInformation("[CustomAuthenticationStateProvider] NotifyUserLogout called");

                // ✅ Trigger re-evaluation of auth state
                // This will cause GetAuthenticationStateAsync to be called again
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogWarning("[CustomAuthenticationStateProvider] User still appears authenticated, logging out...");
                }

                var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                _logger.LogInformation("[CustomAuthenticationStateProvider] ✓ Logout state notified");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CustomAuthenticationStateProvider] Error notifying user logout");
                throw;
            }
        }

        #endregion
    }
}