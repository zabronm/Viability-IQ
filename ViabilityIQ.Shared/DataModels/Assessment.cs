using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels
{
    public class Assessment
    {
        public long ApplicationId { get; set; }
        public string? FileNumber { get; set; }  
        public long LoanTypeId { get; set; }         
        public long FarmerId { get; set; }
        public long FarmId { get; set; }
        public long BankId { get; set; }
        public double AmountRequested { get; set; }
        public double AmountApproved { get; set; }
        public DateTime ApplicationDate { get; set; }
        public int StatusId { get; set; }
        public bool Diligence { get; set; } = false;
        public bool Contracting { get; set; } = false;
        public bool Marketing { get; set; } = false;
        public bool Financing { get; set; } = false;
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public DateTime CapturedDate { get; set; }
        public long CapturedBy { get; set; }
        public DateTime LastModified { get; set; }
        public long LastModifiedBy { get; set; }
    }
}
