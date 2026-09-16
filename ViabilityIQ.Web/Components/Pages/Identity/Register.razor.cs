using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Extensions;

namespace ViabilityIQ.Web.Components.Pages.Identity;

public partial class Register : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public IActivityLogWriter ActivityLogWriter { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public ILogger<Register> Logger { get; set; } = default!;

    public RegisterRequest RegisterRequest { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;
    public bool ShowPassword { get; set; }
    public bool ShowConfirmPassword { get; set; }
    public bool AgreedToTerms { get; set; }
    public bool IsSubmitting { get; set; }
    public bool IsEditProfileMode { get; set; }
    public List<ProvinceLookupDto> Provinces { get; set; } = new();
    public List<BranchLookupDto> Branches { get; set; } = new();
    public List<BranchLookupDto> FilteredBranches { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadLookupsAsync();
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                IsEditProfileMode = true;
                await LoadUserProfileDataAsync(user);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing user account component");
            ErrorMessage = "An error occurred while loading account details.";
        }
    }

    private Task LoadLookupsAsync()
    {
        // Replace these temporary values with database lookup services.
        Provinces = new List<ProvinceLookupDto>
        {
            new() { Id = 1, Name = "Gauteng" },
            new() { Id = 2, Name = "Western Cape" },
            new() { Id = 3, Name = "KwaZulu-Natal" },
            new() { Id = 4, Name = "Gauteng" },
            new() { Id = 5, Name = "Western Cape" },
            new() { Id = 6, Name = "KwaZulu-Natal" }
        };

        Branches = new List<BranchLookupDto>
        {
            new() { Id = 101, ProvinceId = 1, Name = "Johannesburg Central" },
            new() { Id = 102, ProvinceId = 1, Name = "Pretoria Branch" },
            new() { Id = 103, ProvinceId = 2, Name = "Cape Town Waterfront" },
            new() { Id = 104, ProvinceId = 3, Name = "Kuruman/Kimberly" },
            new() { Id = 105, ProvinceId = 4, Name = "Brits/Ga-Rankuwa" },
            new() { Id = 106, ProvinceId = 4, Name = "Mthata/Queberha" },
            new() { Id = 107, ProvinceId = 5, Name = "Welkom" },
            new() { Id = 108, ProvinceId = 6, Name = "Mtubatuba-KZN" }
        };

        return Task.CompletedTask;
    }

    private void OnProvinceChanged()
    {
        FilteredBranches = Branches
            .Where(branch => branch.ProvinceId == RegisterRequest.ProvinceId)
            .ToList();

        if (!FilteredBranches.Any(branch => branch.Id == RegisterRequest.BranchId))
        {
            RegisterRequest.BranchId = 0;
        }
    }

    private async Task LoadUserProfileDataAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        RegisterRequest.Email =
            user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        await Task.CompletedTask;
    }

    public async Task HandleFormSubmit()
    {
        if (IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            if (IsEditProfileMode)
            {
                // Profile persistence is not implemented in the supplied component.
                // Do not write a success-shaped activity until a real update succeeds.
                ErrorMessage = "Profile editing has not yet been connected to a persistence service.";
                return;
            }

            var result = await AuthService.RegisterAsync(RegisterRequest);
            if (!result.Success)
            {
                ErrorMessage = string.Join(" ", result.Messages);
                return;
            }

            SuccessMessage = "Registration successful! Redirecting to login...";

            try
            {
                await ActivityLogWriter.RecordAsync(new ActivityLogWriteRequest
                {
                    Action = ActivityAction.Create,
                    EntityType = nameof(ActivityEntityType.User),
                    EntityId = result.UserId,
                    EntityName = result.Email,
                    UserId = result.UserId,
                    ActorName = $"{RegisterRequest.FirstName} {RegisterRequest.LastName}".Trim(),
                    Module = "Identity",
                    Page = "/identity/register",
                    Remarks = "User account registered successfully.",
                    Metadata = new
                    {
                        RegisterRequest.ProvinceId,
                        RegisterRequest.BranchId
                    }
                });
            }
            catch (Exception auditException)
            {
                Logger.LogError(
                    auditException,
                    "User {UserId} registered, but activity recording failed",
                    result.UserId);
            }

            await Task.Delay(1500);
            Navigation.NavigateTo("/login", replace: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during registration form submission");
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    public bool IsFormValid()
    {
        if (string.IsNullOrWhiteSpace(RegisterRequest.FirstName)
            || string.IsNullOrWhiteSpace(RegisterRequest.LastName)
            || string.IsNullOrWhiteSpace(RegisterRequest.Email)
            || RegisterRequest.ProvinceId <= 0
            || RegisterRequest.BranchId <= 0)
        {
            return false;
        }

        return IsEditProfileMode
            || (!string.IsNullOrWhiteSpace(RegisterRequest.Password)
                && RegisterRequest.Password == RegisterRequest.ConfirmPassword
                && AgreedToTerms);
    }

    public void TogglePasswordVisibility() => ShowPassword = !ShowPassword;
    public void ToggleConfirmPasswordVisibility() => ShowConfirmPassword = !ShowConfirmPassword;
}

public class ProvinceLookupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BranchLookupDto
{
    public long Id { get; set; }
    public long ProvinceId { get; set; }
    public string Name { get; set; } = string.Empty;
}
