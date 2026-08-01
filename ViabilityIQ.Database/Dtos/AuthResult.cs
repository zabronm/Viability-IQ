using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class AuthResult
    {
        public bool Success { get; set; }
        //public string UserId { get; set; }            //==========  If user Id is Guid, then use string
        public long UserId { get; set; }                //==========  If user ID is BigInt in the database, use long instead of string
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; }= string.Empty;
        public List<string> Messages { get; set; } = new();
    }
}
