using Dapper;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;


namespace ViabilityIQ.Application.ServicesMisc
{    
    /// Authentication service for handling user login, registration, and authorization
    /// Designed to work seamlessly with Blazor Server authentication
    
    public class AuthenticationService : IAuthenticationService
    {
        #region Private Fields

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;  // ✅ Dapper-based repository      

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AuthenticationService
        /// </summary>
        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            AuthenticationStateProvider authenticationStateProvider,
            ILogger<AuthenticationService> logger,
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository)  // ✅ Inject the repository
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));  // ✅ Inject the repository
        }

        #endregion

        #region Registration

        /// <summary>
        /// Registers a new user with the provided credentials
        /// </summary>
        /// <param name="request">Registration request containing user details</param>
        /// <returns>Authentication result with success status and messages</returns>
        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var result = new AuthResult();

            try
            {
                _logger.LogInformation("Registration attempt for email: {Email}", request?.Email);

                // Validate request
                if (request == null)
                {
                    result.Success = false;
                    result.Messages.Add("Registration request cannot be null");
                    return result;
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    result.Success = false;
                    result.Messages.Add("Email is already registered");
                    _logger.LogWarning("Registration failed: Email already exists: {Email}", request.Email);
                    return result;
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Create user with password
                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    result.Success = false;
                    result.Messages = createResult.Errors
                        .Select(e => e.Description)
                        .ToList();
                    _logger.LogError("User creation failed for email {Email}: {Errors}",
                        request.Email, string.Join(", ", result.Messages));
                    return result;
                }

                _logger.LogInformation("User created successfully: {Email}, UserId: {UserId}", request.Email, user.Id);

                // Assign default "User" role
                var roleAssignResult = await _userManager.AddToRoleAsync(user, "User");
                if (!roleAssignResult.Succeeded)
                {
                    result.Success = false;
                    result.Messages.Add("Failed to assign user role");
                    _logger.LogError("Failed to assign User role to: {Email}", request.Email);
                    return result;
                }

                result.Success = true;
                result.UserId = user.Id;  // ✅ Direct assignment (user.Id is long)
                result.Email = user.Email;
                result.FirstName = user.FirstName;
                result.Messages.Add("Registration successful. Please log in.");

                _logger.LogInformation("Registration successful for email: {Email}", request.Email);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Registration error: {ex.Message}");
                _logger.LogError(ex, "Exception during registration for email: {Email}", request?.Email);
                return result;
            }
        }

        #endregion

        #region Login

        /// <summary>
        /// Authenticates a user with email and password
        /// Calls the API endpoint to set the authentication cookie
        /// NOTE: Auth state notification is handled by the caller (Login.razor.cs)
        /// </summary>
        /// <param name="request">Login request containing credentials</param>
        /// <returns>Authentication result with success status and user information</returns>
        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var result = new AuthResult();

            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request?.Email);

                if (request == null)
                {
                    result.Success = false;
                    result.Messages.Add("Login request cannot be null");
                    return result;
                }

                // ✅ IMPORTANT: Await each operation fully before starting the next one
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    result.Success = false;
                    result.Messages.Add("Invalid email or password");
                    _logger.LogWarning("Login failed: User not found for email: {Email}", request.Email);
                    return result;
                }

                if (!user.IsActive)
                {
                    result.Success = false;
                    result.Messages.Add("Account is inactive. Please contact administrator.");
                    _logger.LogWarning("Login failed: Inactive account for email: {Email}", request.Email);
                    return result;
                }

                // ✅ Await before next operation
                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                if (isLockedOut)
                {
                    result.Success = false;
                    result.Messages.Add("Account is locked due to multiple failed login attempts. Please try again later.");
                    _logger.LogWarning("Login failed: Account locked for email: {Email}", request.Email);
                    return result;
                }

                // ✅ Await before next operation
                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                {
                    // ✅ Await this operation
                    await _userManager.AccessFailedAsync(user);
                    result.Success = false;
                    result.Messages.Add("Invalid email or password");
                    _logger.LogWarning("Login failed: Invalid password for email: {Email}", request.Email);
                    return result;
                }

                // ✅ Await before next operation
                await _userManager.ResetAccessFailedCountAsync(user);

                if (user.BranchId == null)
                {
                    user.BranchId = 1;
                    _logger.LogInformation("Setting default BranchId for user: {Email}", request.Email);
                }

                user.LastLoginAt = DateTime.UtcNow;

                // ✅ Await this operation
                await _userManager.UpdateAsync(user);

                // ✅ Call the API endpoint to set the authentication cookie
                try
                {
                    _logger.LogInformation("Calling SignIn API endpoint for email: {Email}", request.Email);

                    var request_obj = _httpContextAccessor.HttpContext?.Request;
                    var baseUrl = $"{request_obj?.Scheme}://{request_obj?.Host}";
                    var apiUrl = $"{baseUrl}/api/auth/signin";

                    _logger.LogInformation("API URL: {ApiUrl}", apiUrl);

                    var response = await _httpClient.PostAsJsonAsync(apiUrl, request);

                    _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        result.Success = false;
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Messages.Add($"Sign in failed: {response.StatusCode}");
                        _logger.LogError("SignIn API error: Status={StatusCode}, Content={Content}", response.StatusCode, errorContent);
                        return result;
                    }

                    var jsonContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("✓ SignIn API succeeded - Authentication cookie set");

                    result.Success = true;
                    result.UserId = user.Id;
                    result.Email = user.Email;
                    result.FirstName = user.FirstName;
                    result.Messages.Add("Login successful");

                    _logger.LogInformation("✓✓✓ Login and authentication successful for email: {Email}", request.Email);
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    result.Success = false;
                    result.Messages.Add($"Connection error: {ex.Message}");
                    _logger.LogError(ex, "HttpRequestException during SignIn API call");
                    return result;
                }
                catch (Exception signInEx)
                {
                    result.Success = false;
                    result.Messages.Add($"Sign in error: {signInEx.Message}");
                    _logger.LogError(signInEx, "Exception during SignIn API call for email: {Email}", request.Email);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Login error: {ex.Message}");
                _logger.LogError(ex, "Exception during login for email: {Email}", request?.Email);
                return result;
            }
        }

        #endregion

        #region Logout

        /// <summary>
        /// Logs out the current user
        /// </summary>
        /// <param name="user">The user principal to log out</param>
        public async Task LogoutAsync(ClaimsPrincipal user)
        {
            try
            {
                _logger.LogInformation("Logout requested");
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw new InvalidOperationException("Error during logout", ex);
            }
        }

        #endregion

        #region Authentication State

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        /// <returns>True if user is authenticated, false otherwise</returns>
        public async Task<bool> IsUserAuthenticatedAsync()
        {
            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
                _logger.LogDebug("IsUserAuthenticatedAsync: {IsAuthenticated}", isAuthenticated);
                return isAuthenticated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication state");
                throw new InvalidOperationException("Error checking authentication state", ex);
            }
        }

        /// <summary>
        /// Gets the current authenticated user
        /// NOTE: UserId from claims is string, pass directly to FindByIdAsync
        /// </summary>
        /// <param name="user">The user principal</param>
        /// <returns>ApplicationUser if found, null otherwise</returns>
        public async Task<ApplicationUser> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            try
            {
                if (user?.Identity?.IsAuthenticated == false)
                {
                    _logger.LogDebug("GetCurrentUserAsync: User is not authenticated");
                    return null;
                }

                var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogDebug("GetCurrentUserAsync: No NameIdentifier claim found");
                    return null;
                }

                // ✅ IMPORTANT: Await the operation and don't start another until this completes
                var appUser = await _userManager.FindByIdAsync(userIdString);

                if (appUser != null)
                {
                    _logger.LogDebug("GetCurrentUserAsync: Found user {Email} with UserId {UserId}", appUser.Email, userIdString);
                }
                else
                {
                    _logger.LogDebug("GetCurrentUserAsync: User not found with UserId {UserId}", userIdString);
                }

                return appUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                throw new InvalidOperationException("Error getting current user", ex);
            }
        }

        #endregion

        #region Claims and Roles

        /// <summary>
        /// Gets a specific claim value for the current user
        /// </summary>
        /// <param name="claimType">The type of claim to retrieve</param>
        /// <returns>Claim value if found, empty string otherwise</returns>
        public async Task<string> GetUserClaimAsync(string claimType)
        {
            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var claimValue = authState.User.FindFirst(claimType)?.Value ?? string.Empty;
                _logger.LogDebug("GetUserClaimAsync: Claim {ClaimType} = {ClaimValue}", claimType, claimValue);
                return claimValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user claim: {ClaimType}", claimType);
                throw new InvalidOperationException($"Error getting user claim: {claimType}", ex);
            }
        }

        /// <summary>
        /// Checks if the user has a specific role
        /// </summary>
        /// <param name="user">The user principal</param>
        /// <param name="role">The role to check</param>
        /// <returns>True if user has the role, false otherwise</returns>
        public async Task<bool> HasRoleAsync(ClaimsPrincipal user, string role)
        {
            try
            {
                if (user?.Identity?.IsAuthenticated == false)
                {
                    _logger.LogDebug("HasRoleAsync: User is not authenticated");
                    return false;
                }

                if (string.IsNullOrEmpty(role))
                {
                    _logger.LogDebug("HasRoleAsync: Role is null or empty");
                    return false;
                }

                var appUser = await GetCurrentUserAsync(user);
                if (appUser == null)
                {
                    _logger.LogDebug("HasRoleAsync: User not found in database");
                    return false;
                }

                var hasRole = await _userManager.IsInRoleAsync(appUser, role);
                _logger.LogDebug("HasRoleAsync: User {Email} has role {Role}: {HasRole}", appUser.Email, role, hasRole);
                return hasRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user role: {Role}", role);
                throw new InvalidOperationException($"Error checking user role: {role}", ex);
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// Gets a user by email address
        /// </summary>
        /// <param name="email">The email address</param>
        /// <returns>ApplicationUser if found, null otherwise</returns>
        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogDebug("GetUserByEmailAsync: Email is null or empty");
                    return null;
                }

                var user = await _userManager.FindByEmailAsync(email);
                _logger.LogDebug("GetUserByEmailAsync: Found user for email {Email} with UserId {UserId}", email, user?.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw new InvalidOperationException($"Error getting user by email: {email}", ex);
            }
        }



        #region User Management - NEW DAPPER METHODS

        /// <summary>
        /// Gets a user by email using Dapper (avoids DbContext concurrency)
        /// ✅ USE THIS METHOD INSTEAD OF GetUserByEmailAsync when you have DbContext conflicts
        /// </summary>
        public async Task<ApplicationUser> GetUserByEmailDapperAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogDebug("GetUserByEmailDapperAsync: Email is null or empty");
                    return null;
                }

                _logger.LogInformation("GetUserByEmailDapperAsync: Looking up user by email (Dapper): {Email}", email);

                // ✅ Use Dapper repository instead of UserManager
                var user = await _userRepository.GetUserByEmailAsync(email);

                if (user != null)
                {
                    _logger.LogInformation("GetUserByEmailDapperAsync: Found user {Email} with UserId {UserId}", email, user.Id);
                }
                else
                {
                    _logger.LogWarning("GetUserByEmailDapperAsync: User not found for email: {Email}", email);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByEmailDapperAsync: Error getting user by email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Gets a user by ID using Dapper (avoids DbContext concurrency)
        /// ✅ USE THIS METHOD INSTEAD OF GetUserByIdAsync when you have DbContext conflicts
        /// </summary>
        public async Task<ApplicationUser> GetUserByIdDapperAsync(long userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogDebug("GetUserByIdDapperAsync: UserId is invalid: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("GetUserByIdDapperAsync: Looking up user by ID (Dapper): {UserId}", userId);

                // ✅ Use Dapper repository instead of UserManager
                var user = await _userRepository.GetUserByIdAsync(userId);

                if (user != null)
                {
                    _logger.LogInformation("GetUserByIdDapperAsync: Found user {Email} with UserId {UserId}", user.Email, userId);
                }
                else
                {
                    _logger.LogWarning("GetUserByIdDapperAsync: User not found for ID: {UserId}", userId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByIdDapperAsync: Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        #endregion
    


        /// <summary>
        /// Gets a user by user ID
        /// NOTE: userId is long, but FindByIdAsync expects string, so we convert it
        /// </summary>
        /// <param name="userId">The user ID (long)</param>
        /// <returns>ApplicationUser if found, null otherwise</returns>
        public async Task<ApplicationUser> GetUserByIdAsync(long userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogDebug("GetUserByIdAsync: UserId is invalid: {UserId}", userId);
                    return null;
                }

                // ✅ Convert long to string for FindByIdAsync
                string userIdString = userId.ToString();
                var user = await _userManager.FindByIdAsync(userIdString);

                _logger.LogDebug("GetUserByIdAsync: Found user with ID {UserId}", userId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw new InvalidOperationException($"Error getting user by ID: {userId}", ex);
            }
        }

        #endregion
    }
}
