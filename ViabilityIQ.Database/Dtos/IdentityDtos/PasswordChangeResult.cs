using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos.IdentityDtos
{
    public class PasswordChangeResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }
}
