using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos.IdentityDtos
{
    public class EmailConfirmationResult
    {
        public bool Success { get; set; }
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ConfirmationToken { get; set; }= string.Empty;
        public List<string> Messages { get; set; } = new List<string>();
    }
}
