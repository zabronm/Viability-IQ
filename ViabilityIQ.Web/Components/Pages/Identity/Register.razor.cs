using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Web.Extensions;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Register : ComponentBase
    {
        [Inject] public IAuthenticationService AuthService { get; set; } = default!;
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] public ILogger<Register> Logger { get; set; } = default!;

        public RegisterRequest RegisterRequest { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public bool ShowPassword { get; set; } = false;
        public bool ShowConfirmPassword { get; set; } = false;
        public bool AgreedToTerms { get; set; } = false;
        public bool IsSubmitting { get; set; } = false;
        public bool IsEditProfileMode { get; set; } = false;

        // Dropdown Lookup Lists
        public List<ProvinceLookupDto> Provinces { get; set; } = new();
        public List<BranchLookupDto> Branches { get; set; } = new();
        public List<BranchLookupDto> FilteredBranches { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Load lookup data for dropdowns
                await LoadLookupsAsync();

                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user?.Identity?.IsAuthenticated == true)
                {
                    IsEditProfileMode = true;
                    Logger.LogInformation("Profile page initialized for authenticated user");
                    await LoadUserProfileDataAsync(user);
                }
                else
                {
                    IsEditProfileMode = false;
                    Logger.LogInformation("Registration page initialized");
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
            // Replace with actual service calls to fetch provinces and branches from DB
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

            await Task.CompletedTask;
        }

        private void OnProvinceChanged()
        {
            // Filter branches dynamically when province changes
            FilteredBranches = Branches.Where(b => b.ProvinceId == RegisterRequest.ProvinceId).ToList();

            // Reset branch selection if it no longer belongs to the selected province
            if (!FilteredBranches.Any(b => b.Id == RegisterRequest.BranchId))
            {
                RegisterRequest.BranchId = 0;
            }
            StateHasChanged();
        }

        private async Task LoadUserProfileDataAsync(System.Security.Claims.ClaimsPrincipal user)
        {
            RegisterRequest.Email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
            // Fetch extra attributes and assign to RegisterRequest if editing profile
            await Task.CompletedTask;
        }

        public async Task HandleFormSubmit()
        {
            if (IsSubmitting) return;

            IsSubmitting = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                if (IsEditProfileMode)
                {
                    Logger.LogInformation("Updating profile for: {Email}", RegisterRequest.Email);
                    await Task.Delay(800);
                    SuccessMessage = "Profile updated successfully!";
                }
                else
                {
                    var result = await AuthService.RegisterAsync(RegisterRequest);
                    if (result.Success)
                    {
                        SuccessMessage = "Registration successful! Redirecting to login...";
                        await Task.Delay(1500);
                        Navigation.NavigateTo("/login", replace: true);
                    }
                    else
                    {
                        ErrorMessage = string.Join(" ", result.Messages);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during form submission");
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
            if (string.IsNullOrWhiteSpace(RegisterRequest.FirstName) ||
                string.IsNullOrWhiteSpace(RegisterRequest.LastName) ||
                string.IsNullOrWhiteSpace(RegisterRequest.Email) ||
                RegisterRequest.ProvinceId <= 0 ||
                RegisterRequest.BranchId <= 0)
                return false;

            if (!IsEditProfileMode)
            {
                if (string.IsNullOrWhiteSpace(RegisterRequest.Password) ||
                    RegisterRequest.Password != RegisterRequest.ConfirmPassword ||
                    !AgreedToTerms)
                    return false;
            }

            return true;
        }

        public void TogglePasswordVisibility() => ShowPassword = !ShowPassword;
        public void ToggleConfirmPasswordVisibility() => ShowConfirmPassword = !ShowConfirmPassword;
    }

    // Lookup DTO helpers
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
}