using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels
{
    public class Gender
    {
        public long GenderId { get; set; }
        public string? GenderName { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
    }
}
