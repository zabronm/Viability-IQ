using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Application.Interfaces.IdentityInterfaces
{
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by email using Dapper (not EF Core)
        /// </summary>
        Task<ApplicationUser> GetUserByEmailAsync(string email);

        /// <summary>
        /// Gets a user by ID using Dapper
        /// </summary>
        Task<ApplicationUser> GetUserByIdAsync(long userId);
    }
}

