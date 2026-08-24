using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        
        /// API endpoint to sign in a user
        /// This must be called from an API, not directly from Blazor component
        
        [HttpPost("signin-form")]
        public async Task<IActionResult> SignInForm([FromForm] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Email) || string.IsNullOrEmpty(request?.Password))
                {
                    return Redirect("/identity/login?error=FieldsAreRequired");
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Redirect("/identity/login?error=InvalidCredentials");
                }

                var signInResult = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    request.Password,
                    isPersistent: true,
                    lockoutOnFailure: false);

                if (!signInResult.Succeeded)
                {
                    return Redirect("/identity/login?error=InvalidCredentials");
                }

                // Update last login
                user.LastLoginAt = System.DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Native browser redirect to home with the cookie fully attached
                return LocalRedirect("/home");
            }
    catch(Exception ex)
    {
                _logger.LogError(ex, "Error in SignInForm endpoint");
                return Redirect("/identity/login?error=ServerError");
            }
        }


        
        /// API endpoint to sign out a user
        
        [HttpPost("signout")]
        public async Task<IActionResult> SignOut()
        {
            try
            {
                _logger.LogInformation("SignOut API endpoint called");
                await _signInManager.SignOutAsync();
                _logger.LogInformation("✓ SignOut successful");
                return Ok(new { success = true, message = "Sign out successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignOut API endpoint");
                return StatusCode(500, new { message = $"Sign out error: {ex.Message}" });
            }
        }
    }
}