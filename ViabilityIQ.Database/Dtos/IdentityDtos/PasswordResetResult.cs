using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos.IdentityDtos
{
    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public long UserId { get; set; }
        public string ResetToken { get; set; } = string.Empty;
        public List<string> Messages { get; set; } = new List<string>();
    }
}
