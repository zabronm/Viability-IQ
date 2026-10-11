using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Dtos.IdentityDtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels;
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
        [Inject] public IAuthenticationService AuthService { get; set; } = default!;
        [Inject] public ITenantUserManagementService UserManagementService { get; set; } = default!;
        [Inject] public IGenericDataRepository<Province> ProvinceRepository { get; set; } = default!;
        [Inject] public ISessionService SessionService { get; set; } = default!;

        public UserSettingsRequest SettingsRequest { get; set; } = new();
        public bool IsSubmitting { get; set; } = false;

        public List<ProvinceLookupDto> Provinces { get; set; } = new();
        public List<BranchLookupDto> Branches { get; set; } = new();
        private TenantUserManagementSnapshot? TenantSnapshot { get; set; }
        private TenantMemberRow? CurrentMembership { get; set; }
        private ViabilityIQ.Shared.DataModels.SecurityDataModels.ApplicationUser? CurrentUser { get; set; }

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

            TenantSnapshot = await UserManagementService.GetSnapshotAsync();
            Branches = TenantSnapshot.Branches
                .Select(branch => new BranchLookupDto
                {
                    Id = branch.BranchId,
                    ProvinceId = branch.ProvinceId,
                    Name = branch.BranchName
                })
                .ToList();
        }

        private async Task LoadUserSettingsAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User?.Identity?.IsAuthenticated == true)
            {
                CurrentUser = await AuthService.GetCurrentUserAsync(authState.User);
                CurrentMembership = TenantSnapshot?.Members.SingleOrDefault(
                    member => member.MembershipId == SessionService.TenantMembershipId);
                if (CurrentUser is null || CurrentMembership is null)
                    throw new InvalidOperationException("The current user or tenant membership could not be resolved.");

                SettingsRequest.BranchId = CurrentMembership.BranchId ?? 0;
                OnSiteChanged();
                SettingsRequest.ThemeMode = "light";
                SettingsRequest.Language = "en-US";
                SettingsRequest.SubscriptionPackage = SessionService.SubscriptionPlanCode;
                SettingsRequest.RegistrationDate = CurrentUser.CreatedAt.ToLocalTime();
                SettingsRequest.ExpiryDate = CurrentUser.CreatedAt.ToLocalTime().AddYears(1);
                SettingsRequest.EnableEmailNotifications = true;
                SettingsRequest.EnableSmsNotifications = false;
                SettingsRequest.EnablePhoneCallAlerts = false;
            }
        }

        private void OnSiteChanged()
        {
            SettingsRequest.ProvinceId = Branches
                .SingleOrDefault(branch => branch.Id == SettingsRequest.BranchId)
                ?.ProvinceId ?? 0;
            StateHasChanged();
        }

        public async Task HandleSettingsUpdate()
        {
            if (IsSubmitting) return;

            IsSubmitting = true;

            try
            {
                if (CurrentUser is null || CurrentMembership is null)
                    throw new InvalidOperationException("The current user settings are unavailable.");

                var branchResult = await UserManagementService.UpdateMemberAsync(
                    new UpdateTenantMemberRequest
                    {
                        MembershipId = CurrentMembership.MembershipId,
                        BranchId = SettingsRequest.BranchId,
                        MembershipStatus = CurrentMembership.MembershipStatus,
                        RoleCodes = CurrentMembership.RoleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    });
                if (!branchResult.Succeeded)
                {
                    ToastService.ShowError(branchResult.Message, "Site Update Failed");
                    return;
                }

                OnSiteChanged();

                var profileResult = await AuthService.UpdateProfileAsync(
                    (await AuthStateProvider.GetAuthenticationStateAsync()).User,
                    new RegisterRequest
                    {
                        FirstName = CurrentUser.FirstName,
                        LastName = CurrentUser.LastName,
                        Email = CurrentUser.Email ?? string.Empty,
                        PhoneNumber = CurrentUser.PhoneNumber ?? string.Empty,
                        Department = CurrentUser.Department,
                        JobTitle = CurrentUser.JobTitle,
                        DateOfBirth = CurrentUser.DateOfBirth == default ? null : CurrentUser.DateOfBirth,
                        Address = CurrentUser.Address,
                        City = CurrentUser.City,
                        Country = CurrentUser.Country,
                        ProvinceId = SettingsRequest.ProvinceId
                    });
                if (!profileResult.Success)
                {
                    ToastService.ShowError(
                        profileResult.Messages.Any()
                            ? string.Join(" ", profileResult.Messages)
                            : "The Region could not be updated.",
                        "Settings Partially Updated");
                    return;
                }

                CurrentUser.ProvinceId = SettingsRequest.ProvinceId;
                CurrentMembership = TenantSnapshot!.Members
                    .Single(member => member.MembershipId == CurrentMembership.MembershipId);
                ToastService.ShowSuccess(
                    "Your Region and tenant Site were saved successfully.",
                    "Regional Settings Updated");
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
