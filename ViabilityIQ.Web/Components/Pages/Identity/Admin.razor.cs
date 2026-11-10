using ViabilityIQ.Application.ServicesMisc;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Admin
    {
        [Inject] private ILogger<Admin> Logger { get; set; } = default!;
        [Inject] JSRuntime JS { get; set; } = default!;
        [Inject] private IUserService UserService { get; set; } = default!;
        private List<ApplicationUser> AllUsers = new();
        private Dictionary<string, List<string>> UserRoles = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            try
            {
                AllUsers = (await UserService.GetAllUsersAsync()).ToList();

                foreach (var user in AllUsers)
                {
                    UserRoles[user.Id.ToString()] = await UserService.GetUserRolesAsync(user.Id);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load users and their roles.");
            }
        }

        private void EditUser(ApplicationUser user)
        {
            // Navigate to edit page
        }

        private async Task DeleteUser(ApplicationUser user)
        {
            if (await JS.InvokeAsync<bool>("confirm", $"Are you sure you want to delete {user.FirstName} {user.LastName}?"))
            {
                await UserService.DeleteUserAsync(user.Id);
                await LoadUsers();
            }
        }
    }
}
