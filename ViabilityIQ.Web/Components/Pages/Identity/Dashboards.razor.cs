using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Application.ServicesMisc;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using IAuthenticationService = ViabilityIQ.Application.Interfaces.IdentityInterfaces.IAuthenticationService;


namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Dashboards
    {
        [Inject] private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }
        [Inject] private IAuthenticationService AuthService { get; set; } = null!;
        [Inject] private IUserService UserService { get; set; } = null!;
        [Inject] private NavigationManager Navigation { get; set; } = null!;


        private ApplicationUser? CurrentUser;
        private List<string> UserRoles = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    CurrentUser = await AuthService.GetCurrentUserAsync(user);
                    if (CurrentUser != null)
                    {
                        UserRoles = await UserService.GetUserRolesAsync(CurrentUser.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task HandleLogout()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            await AuthService.LogoutAsync(authState.User);
            Navigation.NavigateTo("/");
        }
    }
}
