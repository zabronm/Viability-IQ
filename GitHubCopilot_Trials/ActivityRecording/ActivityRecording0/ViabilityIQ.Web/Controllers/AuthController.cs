using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAntiforgery _antiforgery;
    private readonly IActivityLogWriter _activityLogWriter;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAntiforgery antiforgery,
        IActivityLogWriter activityLogWriter,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _antiforgery = antiforgery;
        _activityLogWriter = activityLogWriter;
        _logger = logger;
    }

    [HttpPost("signin-form")]
    public async Task<IActionResult> SignInForm([FromForm] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Email)
                || string.IsNullOrWhiteSpace(request.Password))
            {
                return Redirect("/login?error=FieldsAreRequired");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Redirect("/login?error=InvalidCredentials");
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                request.Password,
                request.RememberMe,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                return Redirect("/login?error=InvalidCredentials");
            }

            user.LastLoginAt = DateTime.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning(
                    "Sign-in succeeded but LastLoginAt could not be updated for user {UserId}",
                    user.Id);
            }

            await RecordIdentityActivityAsync(new ActivityLogWriteRequest
            {
                Action = ActivityAction.Login,
                EntityType = nameof(ActivityEntityType.User),
                EntityId = user.Id,
                EntityName = user.Email,
                UserId = user.Id,
                ActorName = GetActorName(user),
                Module = "Identity",
                Page = "/login",
                Remarks = "User signed in successfully.",
                Metadata = new { request.RememberMe }
            });

            return LocalRedirect("/home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SignInForm endpoint");
            return Redirect("/login?error=ServerError");
        }
    }

    [HttpPost("signout-form")]
    public async Task<IActionResult> SignOutForm()
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(ex, "Rejected logout request with an invalid antiforgery token");
            return Redirect("/logout?error=InvalidRequest");
        }

        var currentUser = User.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(User)
            : null;

        try
        {
            if (currentUser == null)
            {
                return LocalRedirect("/login");
            }

            // Logout auditing is mandatory. Persist it while the authenticated
            // request context is still available; if it fails, do not clear the
            // authentication cookie and report an operational error instead.
            await _activityLogWriter.RecordAsync(
                new ActivityLogWriteRequest
                {
                    Action = ActivityAction.Logout,
                    EntityType = nameof(ActivityEntityType.User),
                    EntityId = currentUser.Id,
                    EntityName = currentUser.Email,
                    UserId = currentUser.Id,
                    ActorName = GetActorName(currentUser),
                    Module = "Identity",
                    Page = "/logout",
                    Remarks = "User initiated secure sign out."
                },
                cancellationToken: HttpContext.RequestAborted);

            await _signInManager.SignOutAsync();
            return LocalRedirect("/login");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Logout was not completed because mandatory activity recording or sign-out failed for user {UserId}",
                currentUser?.Id);
            return Redirect("/logout?error=AuditFailure");
        }
    }

    private async Task RecordIdentityActivityAsync(ActivityLogWriteRequest request)
    {
        try
        {
            await _activityLogWriter.RecordAsync(request, cancellationToken: HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // Authentication has already succeeded. Surface the audit failure to
            // operations without converting a valid sign-in into a false failure.
            _logger.LogError(
                ex,
                "Successful {Action} for user {UserId} was not written to the activity log",
                request.Action,
                request.UserId);
        }
    }

    private static string GetActorName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? "User" : fullName;
    }
}
