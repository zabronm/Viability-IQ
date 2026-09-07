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
       
        /// Gets a user by email using Dapper (not EF Core)
       
        Task<ApplicationUser> GetUserByEmailAsync(string email);

       
        /// Gets a user by ID using Dapper
       
        Task<ApplicationUser> GetUserByIdAsync(long userId);
    }
}

