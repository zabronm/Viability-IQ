using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.SharedModels
{
    public class BusinessHealthAlert
    {        
          
        public string AlertId { get; set; }     /// Unique identifier for this alert
        public long AssessmentId { get; set; }   /// Assessment this alert belongs to    
        public AlertSeverityLevel Severity { get; set; } /// Severity level of the alert
        public AlertCategory Category { get; set; } /// Category/type of alert   
        public string Title { get; set; }        /// Alert title (displayed in header)   
        public string Message { get; set; }          /// Alert message (main content)
        public string Recommendation { get; set; }      /// Recommended action
        public List<string> DetailedGuidance { get; set; } = new();      /// Detailed guidance steps/tips
        public decimal MetricValue { get; set; }        /// The actual metric value (e.g., -5000 for negative balance)
        public string MetricLabel { get; set; }      /// Label for the metric (e.g., "Minimum Balance")
        public DateTime GeneratedAt { get; set; }        /// When this alert was generated

        /// Critical alerts cannot be dismissed
        public bool IsDismissible { get; set; } /// Whether user can dismiss this alert
    }


    /// Alert severity levels
    public enum AlertSeverityLevel
    {        
        Healthy = 0,        //All indicators are healthy</summary>       
        Warning = 1,         //Something needs attention</summary>
        Critical = 2        //Urgent action required</summary>
    }

    
    /// Alert categories for grouping and filtering    
    public enum AlertCategory
    {
        CashReserve = 0,        //Related to cash reserves and liquidity</summary>
        Profitability = 1,      //Related to profit margins and ratios</summary>
        Sustainability = 2      //Related to business viability and sustainability</summary>
    }
}
  

