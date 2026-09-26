using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Application.Dtos
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        [Required]
        public string PlanCode { get; set; } = SubscriptionPlanCodes.Standard;

        public string OrganisationName { get; set; } = string.Empty;

        [Range(1, 10000, ErrorMessage = "Seat quantity must be between 1 and 10,000")]
        public int RequestedSeats { get; set; } = 1;

        // Relational numeric keys
        [Range(1, long.MaxValue, ErrorMessage = "Please select a valid province")]
        public long ProvinceId { get; set; }

        public long BranchId { get; set; }

        // Credentials (required only for registration)
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
