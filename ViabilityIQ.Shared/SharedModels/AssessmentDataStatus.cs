using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Shared.SharedModels
{
    public class AssessmentDataStatus
    {
        public long? AssessmentId { get; set; }
        public bool HasAssets { get; set; }
        public bool HasExpenses { get; set; }
        public bool HasSales { get; set; }
        public bool HasStock { get; set; }
        public bool HasReports { get; set; }
        public bool HasReviews { get; set; }
        public bool HasDebtorsCreditors { get; set; }
        public bool HasLoans { get; set; }

       
        /// Count of data types with data (0-8)       
        public int DataTypesWithContent { get; }

       
        /// Percentage of data types loaded (0-100)       
        public decimal CompletionPercentage { get; }

       
        /// Check if assessment has any data       
        public bool HasAnyData { get; }

       
        /// Check if assessment is complete (all 8 data types)       
        public bool IsComplete { get; }

       
        /// Check if assessment is partial (some but not all)       
        public bool IsPartial { get; }

       
        /// Get list of loaded data types       
        public List<string> LoadedDataTypes { get; }

       
        /// Get list of missing data types       
        public List<string> MissingDataTypes { get; }

       
        /// Get status summary string       
        public string GetSummary()
        {
            return $"Assessment {AssessmentId}: {DataTypesWithContent}/8 data types loaded ({CompletionPercentage:F0}%)";
        }
    }
}
