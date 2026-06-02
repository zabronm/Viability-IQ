using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Shared.DataModels
{
    public class AssessmentStatus
    {
        public long Id { get; set; }
        public string? StatusName { get; set; }        
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public long ModifiedBy { get; set; }
    }
}
