using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.SharedModels
{
    // SystemReport - Catalog of all available system reports
    /// Tracks report metadata, configuration, and access control
    [Table("tblSystemReports")]
    public class SystemReports :  IEntity, IAuditableEntity, ISortableEntity
    {
        // ====================================================
        // PRIMARY IDENTIFIERS
        // ====================================================
        [Key]  public long SystemReportId { get; set; }

        // ====================================================
        // REPORT IDENTIFICATION
        // ====================================================
        
        /// Unique report code (e.g., "ASSET_REGISTER", "DEPR_SCHEDULE")        
        public string ReportCode { get; set; } = string.Empty;
        
        /// Display name for the report        
        public string ReportName { get; set; } = string.Empty;    
        
        /// Report category: "Asset", "Assessment", "Financial", "Compliance"        
        public string ReportCategory { get; set; } = string.Empty;

        
        /// Report type: "List", "Summary", "Detail", "Schedule", "Register"        
        public string ReportType { get; set; } = string.Empty;

        // ====================================================
        // REPORT CONFIGURATION
        // ====================================================
        
        /// Path to the report component (e.g., "Pages/Reports/AssetRegisterReport")        
        public string? ComponentPath { get; set; }

        
        /// Display order in report lists        
        public int DisplayOrder { get; set; }

        
        /// Whether the report accepts filter parameters        
        public bool HasParameters { get; set; }

        
        /// JSON schema defining parameters (nullable if no parameters)        
        public string? ParameterSchema { get; set; }

        // ====================================================
        // EXPORT/FORMAT SUPPORT
        // ====================================================
        public bool SupportsPDF { get; set; } = true;
        public bool SupportsExcel { get; set; } = true;
        public bool SupportsCSV { get; set; } = false;
        public bool SupportsEmail { get; set; } = false;
        public bool SupportsScheduling { get; set; } = false;

        // ====================================================
        // PERMISSIONS & ACCESS CONTROL
        // ====================================================
        
        /// Required permission to view report (e.g., "Reports.View.Assets")        
        public string? RequiredPermission { get; set; }

        
        /// Minimum user role required (e.g., "Viewer", "Analyst", "Manager", "Admin")        
        public string? MinimumUserRole { get; set; }

        public bool Active { get; set; } = true;
        public string? Remarks { get; set; }
        /// Description of what the report contains        
      

        // ====================================================
        // AUDIT TRAIL (IAuditableEntity)
        // ====================================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }        


        // ====================================================
        // INTERFACE IMPLEMENTATION (IEntity)
        // ====================================================
        public long Id
        {
            get { return SystemReportId; }
            set { SystemReportId = value; }
        }

        // ====================================================
        // INTERFACE IMPLEMENTATION (IDisplayable)
        // ====================================================
        public string DisplayName => ReportName;

        // ====================================================
        // COMPUTED PROPERTIES
        // ====================================================

        
        /// Export formats supported
        
        public string ExportFormats
        {
            get
            {
                var formats = new System.Collections.Generic.List<string>();
                if (SupportsPDF) formats.Add("PDF");
                if (SupportsExcel) formats.Add("Excel");
                if (SupportsCSV) formats.Add("CSV");
                return string.Join(", ", formats);
            }
        }

        
        /// Full report identifier        
        public string FullIdentifier => $"{ReportCode} - {ReportName}";

      
        long IEntity.Id => SystemReportId;
        string ISortableEntity.DisplayName => ReportName ?? string.Empty;
       
    }
}

