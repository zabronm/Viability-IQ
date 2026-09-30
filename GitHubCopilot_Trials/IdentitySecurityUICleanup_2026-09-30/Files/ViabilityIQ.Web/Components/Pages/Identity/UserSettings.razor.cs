using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos.IdentityDtos;
using ViabilityIQ.Web.Extensions;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class UserSettings
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] public ILogger<Settings> Logger { get; set; } = default!;
        [Inject] public ToastService ToastService { get; set; } = default!;

        public UserSettingsRequest SettingsRequest { get; set; } = new();
        public bool IsSubmitting { get; set; } = false;

        public List<ProvinceLookupDto> Provinces { get; set; } = new();
        public List<BranchLookupDto> Branches { get; set; } = new();
        public List<BranchLookupDto> FilteredBranches { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadLookupsAsync();
                await LoadUserSettingsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing user settings component");
                ToastService.ShowError("An error occurred while loading your settings.", "Settings Unavailable");
            }
        }

        private async Task LoadLookupsAsync()
        {
            Provinces = new List<ProvinceLookupDto>
            {
                new() { Id = 1, Name = "Gauteng" },
                new() { Id = 2, Name = "Western Cape" },
                new() { Id = 3, Name = "KwaZulu-Natal" }
            };

            Branches = new List<BranchLookupDto>
            {
                new() { Id = 101, ProvinceId = 1, Name = "Johannesburg Central" },
                new() { Id = 102, ProvinceId = 1, Name = "Pretoria Branch" },
                new() { Id = 201, ProvinceId = 2, Name = "Cape Town Waterfront" },
                new() { Id = 301, ProvinceId = 3, Name = "Durban CBD" }
            };

            await Task.CompletedTask;
        }

        private async Task LoadUserSettingsAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User?.Identity?.IsAuthenticated == true)
            {
                SettingsRequest.ProvinceId = 1;
                OnProvinceChanged();
                SettingsRequest.BranchId = 101;
                SettingsRequest.ThemeMode = "light";
                SettingsRequest.Language = "en-US";
                SettingsRequest.SubscriptionPackage = "Enterprise";
                SettingsRequest.RegistrationDate = DateTime.Today.AddMonths(-3);
                SettingsRequest.ExpiryDate = DateTime.Today.AddMonths(9);
                SettingsRequest.EnableEmailNotifications = true;
                SettingsRequest.EnableSmsNotifications = true;
                SettingsRequest.EnablePhoneCallAlerts = false;
            }
        }

        private void OnProvinceChanged()
        {
            FilteredBranches = Branches.Where(b => b.ProvinceId == SettingsRequest.ProvinceId).ToList();
            if (!FilteredBranches.Any(b => b.Id == SettingsRequest.BranchId))
            {
                SettingsRequest.BranchId = 0;
            }
            StateHasChanged();
        }

        public async Task HandleSettingsUpdate()
        {
            if (IsSubmitting) return;

            IsSubmitting = true;

            try
            {
                Logger.LogInformation("Saving expanded user settings updates...");
                await Task.Delay(800);
                ToastService.ShowInfo(
                    "Your selections were updated for this session. Persistent preference storage has not yet been configured.",
                    "Settings Updated");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating user settings");
                ToastService.ShowError(
                    "An unexpected error occurred while saving your settings.",
                    "Update Failed");
            }
            finally
            {
                IsSubmitting = false;
                StateHasChanged();
            }
        }

        public bool IsFormValid()
        {
            return SettingsRequest.ProvinceId > 0 &&
                   SettingsRequest.BranchId > 0 &&
                   !string.IsNullOrWhiteSpace(SettingsRequest.ThemeMode) &&
                   !string.IsNullOrWhiteSpace(SettingsRequest.Language) &&
                   !string.IsNullOrWhiteSpace(SettingsRequest.SubscriptionPackage);
        }
    }
}
