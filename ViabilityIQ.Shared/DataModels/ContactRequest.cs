using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Shared.DataModels
{
    public class ContactRequest
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter your company.")]
        public string Company { get; set; } = "";

        [Required(ErrorMessage = "Please select an interest.")]
        public string Interest { get; set; } = "";

        [Required(ErrorMessage = "Please tell us briefly about your project.")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "Please provide at least 10 characters.")]
        public string Message { get; set; } = "";

        public bool Consent { get; set; }
    }
}
