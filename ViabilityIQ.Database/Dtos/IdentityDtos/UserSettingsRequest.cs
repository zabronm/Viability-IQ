using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos.IdentityDtos
{
    public class UserSettingsRequest
    {
        // Regional Assignments
        [Range(1, long.MaxValue, ErrorMessage = "Please select a valid province")]
        public long ProvinceId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Please select a valid branch")]
        public long BranchId { get; set; }

        // Interface & Regional Preferences
        [Required(ErrorMessage = "Theme selection is required")]
        public string ThemeMode { get; set; } = "light";

        [Required(ErrorMessage = "Language preference is required")]
        public string Language { get; set; } = "en-US";

        // Notification Channels
        public bool EnableEmailNotifications { get; set; } = true;
        public bool EnableSmsNotifications { get; set; } = false;
        public bool EnablePhoneCallAlerts { get; set; } = false;

        // Subscription & Licensing Details (Read-only or admin-managed view)
        [Required(ErrorMessage = "Subscription package is required")]
        public string SubscriptionPackage { get; set; } = "Standard";

        public DateTime RegistrationDate { get; set; } = DateTime.Today;
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);
    }
}
