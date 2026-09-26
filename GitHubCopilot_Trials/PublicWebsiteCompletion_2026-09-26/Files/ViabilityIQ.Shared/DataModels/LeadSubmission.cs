using Dapper.Contrib.Extensions;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("LeadSubmissions")]
    public class LeadSubmission
    {
        [Key] public long LeadSubmissionId { get; set; }
        public string? FullName { get; set; }       
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }        
        public string? Country { get; set; }        
        public string? City { get; set; }
        public string AssetVolumeScale { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "New";
        public DateTime SubmittedAtUtc { get; set; }
        public DateTime? EmailSentAtUtc { get; set; }
    }
}
