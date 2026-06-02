using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels
{
    public class Race
    {
        public long RaceId { get; set; }
        public string? RaceName { get; set; }
        public bool Active { get; set; }
        public string? Remarks { get; set; }
    }
}
