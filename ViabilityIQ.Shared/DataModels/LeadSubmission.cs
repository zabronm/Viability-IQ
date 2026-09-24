using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblLeadSubmissions")]
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
    }
}
