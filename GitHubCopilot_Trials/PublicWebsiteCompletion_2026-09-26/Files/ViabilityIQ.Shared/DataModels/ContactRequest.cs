using System.ComponentModel.DataAnnotations;

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

        [StringLength(150)]
        public string Company { get; set; } = "";

        [RegularExpression(
            @"^$|^\+?[0-9 ()-]{7,30}$",
            ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Please select a subject.")]
        [RegularExpression(
            "^(Feedback|Support|Demo Request|Sales Enquiry|Partnership|Other)$",
            ErrorMessage = "Please select a valid subject.")]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Please enter your message.")]
        [StringLength(2000, MinimumLength = 10,
            ErrorMessage = "Please provide between 10 and 2,000 characters.")]
        public string Message { get; set; } = "";

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Please agree to us processing this enquiry.")]
        public bool Consent { get; set; }

        public string Website { get; set; } = "";
    }
}
