using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.SecurityDataModels
{
    public class ApplicationUser: IdentityUser<long>
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public long? ProvinceId { get; set; }
        public long? BranchId { get; set; }
        public string Department { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string PhoneNumberPersonal { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PreviousLoginAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastLogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; }

        // Navigation properties
        //public ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
    }
}
