Use ViabilityIQ;

CREATE TABLE [dbo].[tblAssessmentReviews]
(
    -- ====================================================
    -- PRIMARY IDENTIFIERS
    -- ====================================================
    [AssessmentReviewId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AssessmentId] BIGINT NOT NULL,
    [ReviewedBy] BIGINT NOT NULL,

    -- ====================================================
    -- REVIEW IDENTIFICATION
    -- ====================================================
    [ReviewType] NVARCHAR(50) NOT NULL,        -- "Initial", "Technical", "Financial", "Legal", "Final"
    [ReviewStatus] NVARCHAR(50) NOT NULL,      -- "Pending", "In-Progress", "Completed", "Rejected", "Approved"
    [ReviewRound] INT NOT NULL DEFAULT 1,      -- 1st review, 2nd review, etc.

    -- ====================================================
    -- REVIEW DATES
    -- ====================================================
    [ReviewStartDate] DATETIME2 NOT NULL,
    [ReviewDueDate] DATETIME2,
    [ReviewCompletedDate] DATETIME2,
    [ReviewDuration] INT,                      -- Duration in minutes (computed or stored)

    -- ====================================================
    -- REVIEW FINDINGS
    -- ====================================================
    [OverallRating] NVARCHAR(20),               -- "Excellent", "Good", "Satisfactory", "Needs Improvement", "Critical"
    [ComplianceScore] DECIMAL(5,2),             -- 0-100 percentage
    [AccuracyRating] NVARCHAR(20),              -- "High", "Acceptable", "Low", "Critical Issues"
    [CompletenessRating] NVARCHAR(20),          -- "Complete", "Minor Gaps", "Major Gaps", "Incomplete"

    -- ====================================================
    -- REVIEW COMMENTS & FINDINGS
    -- ====================================================
    [ReviewComments] NVARCHAR(MAX),             -- Overall review summary
    [FindingsSummary] NVARCHAR(MAX),            -- Summary of key findings
    [IssuesIdentified] INT DEFAULT 0,           -- Count of issues found
    [CriticalIssues] INT DEFAULT 0,             -- Count of critical issues
    [MinorIssues] INT DEFAULT 0,                -- Count of minor issues

    -- ====================================================
    -- RECOMMENDATIONS
    -- ====================================================
    [Recommendations] NVARCHAR(MAX),            -- Suggested improvements
    [RecommendedActions] NVARCHAR(MAX),         -- Required follow-up actions
    [ActionItems] INT DEFAULT 0,                -- Number of action items generated

    -- ====================================================
    -- APPROVAL & SIGN-OFF
    -- ====================================================
    [ApprovalStatus] NVARCHAR(50),              -- "Pending", "Approved", "Approved with Comments", "Rejected", "Conditional"
    [ApprovedBy] BIGINT,                        -- User who approved
    [ApprovalDate] DATETIME2,
    [ApprovalComments] NVARCHAR(MAX),
    [RequestedChanges] NVARCHAR(MAX),           -- If rejected/conditional
    [ResubmitRequired] BIT DEFAULT 0,

    -- ====================================================
    -- REVIEW SCOPE
    -- ====================================================
    [AssetsReviewedCount] INT,                  -- Number of assets reviewed
    [AssetsWithIssues] INT,                     -- Assets that had issues
    [DataSourcesReviewed] NVARCHAR(MAX),        -- JSON array of sources checked
    [SamplingApproach] NVARCHAR(500),           -- "100% Review", "Statistical Sample", "Risk-Based"
    [SamplingSize] INT,                         -- If applicable
    [AreasReviewed] NVARCHAR(MAX),               -- JSON array of review areas

    -- ====================================================
    -- ATTACHMENTS & EVIDENCE
    -- ====================================================
    [EvidenceDocuments] NVARCHAR(MAX),          -- JSON array of document references
    [SupportingAttachments] INT DEFAULT 0,      -- Count of attached files
    [ReviewTemplate] NVARCHAR(500),             -- Template used for review

    -- ====================================================
    -- ESCALATION & FOLLOW-UP
    -- ====================================================
    [IsEscalated] BIT DEFAULT 0,                -- Whether review needs escalation
    [EscalatedTo] BIGINT,                       -- Manager/supervisor ID
    [EscalationReason] NVARCHAR(MAX),
    [EscalationDate] DATETIME2,
    [FollowUpRequired] BIT DEFAULT 0,
    [FollowUpDueDate] DATETIME2,
    [FollowUpCompletedDate] DATETIME2,

    -- ====================================================
    -- METADATA & TRACKING
    -- ====================================================
    [ReviewNotes] NVARCHAR(MAX),                -- Internal notes (not for assessment owner)
    [IsConflictOfInterest] BIT DEFAULT 0,       -- If reviewer has conflict
    [ConflictDescription] NVARCHAR(MAX),
    [ReviewMethod] NVARCHAR(100),               -- "In-Person", "Remote", "Hybrid", "Document-Based"
    [MeetingDate] DATETIME2,                    -- If in-person review meeting
    [MeetingAttendees] NVARCHAR(MAX),            -- JSON array of attendees

    -- ====================================================
    -- AUDIT & TRACKING
    -- ====================================================
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME2,
    [ModifiedBy] BIGINT,
    [Active] BIT NOT NULL DEFAULT 1,

    -- ====================================================
    -- INDEXES
    -- ====================================================
    INDEX IX_AssessmentId NONCLUSTERED ([AssessmentId]),
    INDEX IX_ReviewedBy NONCLUSTERED ([ReviewedBy]),
    INDEX IX_ReviewStatus NONCLUSTERED ([ReviewStatus]),
    INDEX IX_ApprovalStatus NONCLUSTERED ([ApprovalStatus]),
    INDEX IX_ReviewType NONCLUSTERED ([ReviewType]),
    INDEX IX_ReviewStartDate NONCLUSTERED ([ReviewStartDate]),
    INDEX IX_IsEscalated NONCLUSTERED ([IsEscalated])
);


-- ==========================================================

CREATE TABLE [dbo].[tblAssessmentReviewIssues]
(
    -- ====================================================
    -- PRIMARY IDENTIFIERS
    -- ====================================================
    [AssessmentReviewIssueId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AssessmentReviewId] BIGINT NOT NULL,
    [AssetId] BIGINT,                           -- Specific asset with issue (nullable)

    -- ====================================================
    -- ISSUE DETAILS
    -- ====================================================
    [IssueSeverity] NVARCHAR(20) NOT NULL,      -- "Critical", "High", "Medium", "Low"
    [IssueCategory] NVARCHAR(100) NOT NULL,    -- "Missing Data", "Data Quality", "Policy Violation", "Compliance Gap", "Calculation Error"
    [IssueTitle] NVARCHAR(200) NOT NULL,
    [IssueDescription] NVARCHAR(MAX),
    [FindingDetails] NVARCHAR(MAX),             -- Detailed explanation of finding

    -- ====================================================
    -- RESOLUTION
    -- ====================================================
    [CorrectiveAction] NVARCHAR(MAX),           -- Required corrective action
    [ActionAssignedTo] BIGINT,                  -- User responsible for correction
    [DueDate] DATETIME2,
    [ResolutionStatus] NVARCHAR(50),            -- "Open", "In-Progress", "Resolved", "Waived"
    [ResolutionDate] DATETIME2,
    [ResolutionNotes] NVARCHAR(MAX),
    [VerifiedBy] BIGINT,                        -- Who verified the fix
    [VerificationDate] DATETIME2,

    -- ====================================================
    -- EVIDENCE
    -- ====================================================
    [EvidenceAttached] BIT DEFAULT 0,
    [ReferenceDocuments] NVARCHAR(MAX),         -- JSON array
    [ScreenshotOrLink] NVARCHAR(MAX),           -- Link to affected data

    -- ====================================================
    -- AUDIT TRAIL
    -- ====================================================
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME2,
    [ModifiedBy] BIGINT,
    [Active] BIT NOT NULL DEFAULT 1,

    -- ====================================================
    -- INDEXES
    -- ====================================================
    INDEX IX_AssessmentReviewId NONCLUSTERED ([AssessmentReviewId]),
    INDEX IX_IssueSeverity NONCLUSTERED ([IssueSeverity]),
    INDEX IX_ResolutionStatus NONCLUSTERED ([ResolutionStatus])
);

