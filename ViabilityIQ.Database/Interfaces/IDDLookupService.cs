using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IDDLookupService
    {
        Task<IEnumerable<LookupItem>> GetLookupOptionsAsync(DDLookupEnums lookupKey);
    }

    public class LookupItem
    {
        public long Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

}
