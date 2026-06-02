using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class BankDto
    {
        public long BankId { get; set; }
        public string? BankName { get; set; }
        public string? ShortName { get; set; }
        public bool? Active { get; set; }
        public string? Remarks { get; set; }
    }
}
