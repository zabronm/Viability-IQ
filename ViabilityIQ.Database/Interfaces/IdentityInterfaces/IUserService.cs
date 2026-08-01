using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;


namespace ViabilityIQ.Application.Interfaces.IdentityInterfaces
{
    public  interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(long userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(long userId);
        Task<List<string>> GetUserRolesAsync(long userId);
        Task<bool> AssignRoleAsync(long userId, string roleName);
        Task<bool> RemoveRoleAsync(long userId, string roleName);
    }
}
