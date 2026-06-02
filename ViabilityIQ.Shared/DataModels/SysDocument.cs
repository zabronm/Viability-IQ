using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels
{
    public class SysDocument
    {
        public long DocumentId { get; set; }        
        public string? DocumentName { get; set; }        
        public string? Remarks { get; set; }
        public bool? Active { get; set; }
        public DateTime CapturedDate { get; set; }
        public long CapturedBy { get; set; }
        public DateTime LastModified { get; set; }
        public long LastModifiedBy { get; set; }
    }
}
