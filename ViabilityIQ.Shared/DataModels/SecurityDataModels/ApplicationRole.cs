using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.SecurityDataModels
{
    public class ApplicationRole: IdentityRole<long>
    {
        public string Description { get; set; }= string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        //public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
