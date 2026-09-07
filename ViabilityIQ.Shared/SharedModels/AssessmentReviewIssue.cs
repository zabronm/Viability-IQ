using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.SharedModels
{

    [Table("tblAssessmentReviewIssues")]
    public class AssessmentReviewIssue : IEntity, IAuditableEntity, ISortableEntity
    {
        // ====================================================
        // PRIMARY IDENTIFIERS
        // ====================================================
        [Key]        public long AssessmentReviewIssueId { get; set; }

        public long AssessmentReviewId { get; set; }

        public long? AssessmentId { get; set; }

        // ====================================================
        // ISSUE DETAILS
        // ====================================================
        
        /// Severity level: "Critical", "High", "Medium", "Low"
        
        public string IssueSeverity { get; set; } = string.Empty;

        
        /// Category: "Missing Data", "Data Quality", "Policy Violation", etc.
        
        public string IssueCategory { get; set; } = string.Empty;

        public string IssueTitle { get; set; } = string.Empty;

        public string? IssueDescription { get; set; }

        public string? FindingDetails { get; set; }

        // ====================================================
        // RESOLUTION
        // ====================================================
        public string? CorrectiveAction { get; set; }

        public long? ActionAssignedTo { get; set; }

        public DateTime? DueDate { get; set; }

        
        /// Status: "Open", "In-Progress", "Resolved", "Waived"
        
        public string? ResolutionStatus { get; set; }

        public DateTime? ResolutionDate { get; set; }

        public string? ResolutionNotes { get; set; }

        public long? VerifiedBy { get; set; }

        public DateTime? VerificationDate { get; set; }

        // ====================================================
        // EVIDENCE
        // ====================================================
        public bool EvidenceAttached { get; set; }

        public string? ReferenceDocuments { get; set; }  // JSON

        public string? ScreenshotOrLink { get; set; }

        // ====================================================
        // AUDIT TRAIL (IAuditableEntity)
        // ====================================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public long CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; } =  DateTime.UtcNow;

        public long ModifiedBy { get; set; }

        public bool Active { get; set; }
        public string Remarks { get; set; } = string.Empty;

        // ====================================================
        // INTERFACE IMPLEMENTATION (IEntity)
        // ====================================================
        public long Id
        {
            get { return AssessmentReviewIssueId; }
            set { AssessmentReviewIssueId = value; }
        }

        // ====================================================
        // COMPUTED PROPERTIES
        // ====================================================

        
        /// Is issue overdue for resolution?
        
        public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate && ResolutionStatus != "Resolved";

        
        /// Is critical issue?
        
        public bool IsCritical => IssueSeverity == "Critical";

        
        /// Issue severity color for UI
        
        public string SeverityBadgeClass
        {
            get
            {
                return IssueSeverity switch
                {
                    "Critical" => "bg-danger text-white",
                    "High" => "bg-warning text-black",
                    "Medium" => "bg-info text-black",
                    "Low" => "bg-secondary text-white",
                    _ => "bg-light"
                };
            }
        }


        long IEntity.Id => AssessmentReviewIssueId;
        string ISortableEntity.DisplayName => IssueTitle ?? string.Empty;
    }
}

