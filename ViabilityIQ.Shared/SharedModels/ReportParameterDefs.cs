using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.SharedModels
{
    public class ReportParameterDefs
    {
        public string Key { get; set; } = string.Empty;       // E.g., "StartDate", "AsAtDate"
        public string Label { get; set; } = string.Empty;     // E.g., "From Date", "Reporting Date"
        public ReportParameter_Enums Type { get; set; }
        public bool IsRequired { get; set; } = true;
        public string? DefaultValue { get; set; }
    }
}
