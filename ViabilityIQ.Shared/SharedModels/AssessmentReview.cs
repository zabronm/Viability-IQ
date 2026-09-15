using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    
    /// AssessmentReview - Tracks review and approval workflow for assessments
    /// Supports multi-round reviews, escalations, and compliance tracking
    
    [Table("tblAssessmentReviews")]
    public class AssessmentReview : IEntity, IAuditableEntity, ISortableEntity
    {
        // ====================================================
        // PRIMARY IDENTIFIERS
        // ====================================================
        [Key]        public long AssessmentReviewId { get; set; }

        public long AssessmentId { get; set; }

        public long ReviewedBy { get; set; }

        // ====================================================
        // REVIEW IDENTIFICATION
        // ====================================================
        
        /// Type of review: "Initial", "Technical", "Financial", "Legal", "Final"        
        public string ReviewType { get; set; } = string.Empty;

        
        /// Current status: "Pending", "In-Progress", "Completed", "Rejected", "Approved"        
        public string ReviewStatus { get; set; } = string.Empty;

        
        /// Review round number (1st, 2nd, 3rd review, etc.)        
        public int ReviewRound { get; set; } = 1;

        // ====================================================
        // REVIEW DATES
        // ====================================================
        public DateTime ReviewStartDate { get; set; }
        public DateTime? ReviewDueDate { get; set; }
        public DateTime? ReviewCompletedDate { get; set; }
        public int? ReviewDuration { get; set; }  // Minutes

        // ====================================================
        // REVIEW FINDINGS
        // ====================================================
        
        /// Overall assessment: "Excellent", "Good", "Satisfactory", "Needs Improvement", "Critical"        
        public string? OverallRating { get; set; }

        
        /// Compliance score (0-100)        
        public decimal? ComplianceScore { get; set; }

        
        /// Accuracy assessment: "High", "Acceptable", "Low", "Critical Issues"        
        public string? AccuracyRating { get; set; }

        
        /// Completeness assessment: "Complete", "Minor Gaps", "Major Gaps", "Incomplete"        
        public string? CompletenessRating { get; set; }

        // ====================================================
        // REVIEW COMMENTS & FINDINGS
        // ====================================================
        public string? ReviewComments { get; set; }
        public string? FindingsSummary { get; set; }
        public int IssuesIdentified { get; set; }
        public int CriticalIssues { get; set; }
        public int MinorIssues { get; set; }

        // ====================================================
        // RECOMMENDATIONS
        // ====================================================
        public string? Recommendations { get; set; }
        public string? RecommendedActions { get; set; }
        public int ActionItems { get; set; }

        // ====================================================
        // APPROVAL & SIGN-OFF
        // ====================================================
        
        /// Status: "Pending", "Approved", "Approved with Comments", "Rejected", "Conditional"
        
        public string? ApprovalStatus { get; set; }
        public long? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? ApprovalComments { get; set; }
        public string? RequestedChanges { get; set; }
        public bool ResubmitRequired { get; set; }

        // ====================================================
        // REVIEW SCOPE
        // ====================================================
        public int? AssetsReviewedCount { get; set; }
        public int? AssetsWithIssues { get; set; }
        public string? DataSourcesReviewed { get; set; }  // JSON
        public string? SamplingApproach { get; set; }
        public int? SamplingSize { get; set; }
        public string? AreasReviewed { get; set; }  // JSON

        // ====================================================
        // ATTACHMENTS & EVIDENCE
        // ====================================================
        public string? EvidenceDocuments { get; set; }  // JSON
        public int SupportingAttachments { get; set; }
        public string? ReviewTemplate { get; set; }

        // ====================================================
        // ESCALATION & FOLLOW-UP
        // ====================================================
        public bool IsEscalated { get; set; }
        public long? EscalatedTo { get; set; }
        public string? EscalationReason { get; set; }
        public DateTime? EscalationDate { get; set; }
        public bool FollowUpRequired { get; set; }
        public DateTime? FollowUpDueDate { get; set; }
        public DateTime? FollowUpCompletedDate { get; set; }

        // ====================================================
        // METADATA & TRACKING
        // ====================================================
        public string? ReviewNotes { get; set; }
        public bool IsConflictOfInterest { get; set; }
        public string? ConflictDescription { get; set; }
        public string? ReviewMethod { get; set; }  // "In-Person", "Remote", "Hybrid", "Document-Based"
        public DateTime? MeetingDate { get; set; }
        public string? MeetingAttendees { get; set; }  // JSON

        // ====================================================
        // AUDIT TRAIL (IAuditableEntity)
        // ====================================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }
        public bool Active { get; set; } = true;
        public string Remarks { get; set; } = string.Empty;

        // ====================================================
        // INTERFACE IMPLEMENTATION (IEntity)
        // ====================================================
        public long Id
        {
            get { return AssessmentReviewId; }
            set { AssessmentReviewId = value; }
        }

        // ====================================================
        // COMPUTED PROPERTIES
        // ====================================================

        
        /// Is review overdue?        
        public bool IsOverdue => ReviewDueDate.HasValue && DateTime.UtcNow > ReviewDueDate && ReviewStatus != "Completed";

        
        /// Has critical issues?        
        public bool HasCriticalIssues => CriticalIssues > 0;

        
        /// Review completion percentage        
        public int CompletionPercentage
        {
            get
            {
                if (ReviewStatus == "Completed") return 100;
                if (ReviewStatus == "In-Progress") return 50;
                if (ReviewStatus == "Pending") return 0;
                return 0;
            }
        }

        
        /// Review status color for UI        
        public string StatusBadgeClass
        {
            get
            {
                return ReviewStatus switch
                {
                    "Pending" => "bg-secondary",
                    "In-Progress" => "bg-info",
                    "Completed" => "bg-success",
                    "Rejected" => "bg-danger",
                    "Approved" => "bg-success",
                    _ => "bg-light"
                };
            }
        }

        long IEntity.Id => AssessmentReviewId;
        string ISortableEntity.DisplayName => ReviewType ?? string.Empty;

    }   
}