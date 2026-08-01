using ViabilityIQ.Application.ServicesMisc;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ViabilityIQ.Application.ServicesMisc;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using Microsoft.JSInterop;

namespace ViabilityIQ.Web.Components.Pages.Identity
{
    public partial class Admin
    {
        [Inject] JSRuntime? JS { get; set; }
        [Inject] private IUserService? UserService { get; set; } 
        private List<ApplicationUser>? AllUsers;
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
                // Handle error
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
