using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;
using System.Text.Encodings.Web;



namespace ViabilityIQ.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogWriter _activityLogWriter;
    private readonly ILogger<AuthController> _logger;
    private readonly IAntiforgery _antiforgery;


    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IActivityLogWriter activityLogWriter,
        IAntiforgery antiforgery,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _activityLogWriter = activityLogWriter;
        _antiforgery = antiforgery;
        _logger = logger;
    }


    //=====  SIGN IN PROCEDURE  =====//
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


    //=====  SIGN OUT PROCEDURE  =====//
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
            return Redirect("/api/auth/signout-confirmation?error=InvalidRequest");
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
            return Redirect("/api/auth/signout-confirmation?error=AuditFailure");
        }
    }





    //=====  SIGN OUT FORM PROCEDURE  =====//
    [HttpGet("signout-confirmation")]
    public IActionResult SignOutConfirmation([FromQuery] string? error = null)
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        var encoder = HtmlEncoder.Default;
        var requestToken = encoder.Encode(tokens.RequestToken ?? string.Empty);
        var formFieldName = encoder.Encode(tokens.FormFieldName);
        var errorMarkup = error switch
        {
            "AuditFailure" =>
                """
            <div class="alert">
                Sign out was stopped because the mandatory activity record
                could not be saved. Please retry or contact support.
            </div>
            """,
            "InvalidRequest" =>
                """
            <div class="alert">
                The sign-out request expired or was invalid. Please submit it again.
            </div>
            """,
            _ => string.Empty
        };

        var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Sign out - Viability.IQ</title>
            <style>
                body {
                    margin: 0;
                    font-family: Arial, sans-serif;
                    color: #334155;
                    background: #f5f7f9;
                }
                main {
                    display: grid;
                    min-height: 100vh;
                    place-items: center;
                    padding: 24px;
                }
                section {
                    width: min(420px, 100%);
                    padding: 28px;
                    box-sizing: border-box;
                    border: 1px solid #dce3e8;
                    border-radius: 9px;
                    background: #fff;
                    box-shadow: 0 5px 18px rgba(15, 42, 67, .08);
                    text-align: center;
                }
                h1 { margin: 0 0 8px; font-size: 20px; color: #1e3a5f; }
                p { margin: 0 0 20px; font-size: 13px; color: #718096; }
                .alert {
                    margin: 0 0 16px;
                    padding: 10px;
                    border: 1px solid #e7aeb5;
                    border-radius: 5px;
                    color: #8f2f3c;
                    background: #fff4f5;
                    font-size: 12px;
                    text-align: left;
                }
                .actions { display: flex; justify-content: center; gap: 8px; }
                button, a {
                    padding: 7px 15px;
                    border-radius: 5px;
                    font-size: 12px;
                    text-decoration: none;
                    cursor: pointer;
                }
                button {
                    border: 1px solid #b02a37;
                    color: #fff;
                    background: #dc3545;
                }
                a {
                    border: 1px solid #adb5bd;
                    color: #495057;
                    background: #fff;
                }
            </style>
        </head>
        <body>
            <main>
                <section>
                    <h1>Sign out of Viability.IQ?</h1>
                    <p>Your current session will be closed securely.</p>
                    {{errorMarkup}}
                    <form method="post" action="/api/auth/signout-form">
                        <input type="hidden"
                               name="{{formFieldName}}"
                               value="{{requestToken}}">
                        <div class="actions">
                            <button type="submit">Sign out</button>
                            <a href="/home">Cancel</a>
                        </div>
                    </form>
                </section>
            </main>
        </body>
        </html>
        """;

        return Content(html, "text/html; charset=utf-8");
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
