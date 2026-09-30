using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Extensions;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.Identity;

public partial class Register : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public IActivityLogWriter ActivityLogWriter { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public ILogger<Register> Logger { get; set; } = default!;
    [Inject] public IGenericDataRepository<Province> ProvinceRepository { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    public RegisterRequest RegisterRequest { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;
    public bool ShowPassword { get; set; }
    public bool ShowConfirmPassword { get; set; }
    public bool AgreedToTerms { get; set; }
    public bool IsSubmitting { get; set; }
    public bool IsEditProfileMode { get; set; }
    public List<ProvinceLookupDto> Provinces { get; set; } = new();
    public bool IsOpenPlan =>
        RegisterRequest.PlanCode == SubscriptionPlanCodes.Open;

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

    private async Task LoadLookupsAsync()
    {
        var provinces = await ProvinceRepository.GetAllAsync();
        Provinces = provinces
            .Where(province => province.Active)
            .OrderBy(province => province.ProvinceName)
            .Select(province => new ProvinceLookupDto
            {
                Id = province.ProvinceId,
                Name = province.ProvinceName ?? string.Empty
            })
            .ToList();
    }

    private void SelectPlan(string planCode)
    {
        RegisterRequest.PlanCode = planCode;
        if (planCode == SubscriptionPlanCodes.Standard)
        {
            RegisterRequest.BranchId = 0;
            RegisterRequest.OrganisationName = string.Empty;
            RegisterRequest.RequestedSeats = 1;
        }
        else
        {
            RegisterRequest.RequestedSeats = Math.Max(2, RegisterRequest.RequestedSeats);
        }
    }

    private async Task LoadUserProfileDataAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        var applicationUser = await AuthService.GetCurrentUserAsync(user);
        if (applicationUser is null)
            throw new InvalidOperationException("The authenticated user profile could not be resolved.");

        RegisterRequest.FirstName = applicationUser.FirstName;
        RegisterRequest.LastName = applicationUser.LastName;
        RegisterRequest.Email = applicationUser.Email ?? string.Empty;
        RegisterRequest.PhoneNumber = applicationUser.PhoneNumber ?? string.Empty;
        RegisterRequest.Department = applicationUser.Department;
        RegisterRequest.JobTitle = applicationUser.JobTitle;
        RegisterRequest.DateOfBirth =
            applicationUser.DateOfBirth == default ? null : applicationUser.DateOfBirth;
        RegisterRequest.Address = applicationUser.Address;
        RegisterRequest.City = applicationUser.City;
        RegisterRequest.Country = applicationUser.Country;
        RegisterRequest.ProvinceId = applicationUser.ProvinceId ?? 0;
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
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var updateResult = await AuthService.UpdateProfileAsync(
                    authState.User,
                    RegisterRequest);
                if (!updateResult.Success)
                {
                    ToastService.ShowError(
                        updateResult.Messages.Any()
                            ? string.Join(" ", updateResult.Messages)
                            : "Your profile could not be updated.",
                        "Profile Update Failed");
                    return;
                }

                ToastService.ShowSuccess("Your profile was updated successfully.", "Profile Updated");
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
                        RegisterRequest.PlanCode,
                        RegisterRequest.OrganisationName,
                        RegisterRequest.RequestedSeats,
                        result.TenantId
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
            || RegisterRequest.ProvinceId <= 0)
        {
            return false;
        }

        if (IsOpenPlan
            && (string.IsNullOrWhiteSpace(RegisterRequest.OrganisationName)
                || RegisterRequest.RequestedSeats < 2))
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
