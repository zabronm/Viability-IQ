using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.SharedModels
{
    public class ReportDefs
    {
        public string Id { get; set; } = string.Empty;        // E.g., "SALES_RPT", "BALANCE_SHEET"
        public string Name { get; set; } = string.Empty;      // E.g., "Sales Performance Report"
        public string Description { get; set; } = string.Empty;
        public List<ReportParameterDefs> Parameters { get; set; } = new();
    }
}

