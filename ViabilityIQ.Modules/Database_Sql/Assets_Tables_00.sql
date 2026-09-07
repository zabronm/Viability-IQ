Use ViabilityIQ;

CREATE TABLE [dbo].[tblSystemReports]
(
    -- ====================================================
    -- PRIMARY IDENTIFIERS
    -- ====================================================
    [SystemReportId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),

    -- ====================================================
    -- REPORT IDENTIFICATION
    -- ====================================================
    [ReportCode] NVARCHAR(50) NOT NULL UNIQUE,
    [ReportName] NVARCHAR(200) NOT NULL,    
    [ReportCategory] NVARCHAR(100) NOT NULL,  -- e.g., "Asset", "Assessment", "Financial", "Compliance"
    [ReportType] NVARCHAR(50) NOT NULL,       -- e.g., "List", "Summary", "Detail", "Schedule", "Register"

    -- ====================================================
    -- REPORT CONFIGURATION
    -- ====================================================
    [ComponentPath] NVARCHAR(500),             -- e.g., "Pages/Reports/AssetRegisterReport"
    [IsActive] BIT NOT NULL DEFAULT 1,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [HasParameters] BIT NOT NULL DEFAULT 0,    -- True if report accepts filters/parameters
    [ParameterSchema] NVARCHAR(MAX),           -- JSON storing parameter structure
    
    -- ====================================================
    -- EXPORT/FORMAT SUPPORT
    -- ====================================================
    [SupportsPDF] BIT NOT NULL DEFAULT 1,
    [SupportsExcel] BIT NOT NULL DEFAULT 1,
    [SupportsCSV] BIT NOT NULL DEFAULT 0,
    [SupportsEmail] BIT NOT NULL DEFAULT 0,
    [SupportsScheduling] BIT NOT NULL DEFAULT 0,

    -- ====================================================
    -- PERMISSIONS & ACCESS CONTROL
    -- ====================================================
    [RequiredPermission] NVARCHAR(100),        -- e.g., "Reports.View.Assets", "Reports.View.Financial"
    [MinimumUserRole] NVARCHAR(50),            -- e.g., "Viewer", "Analyst", "Manager", "Admin"

    [Active] BIT NOT NULL DEFAULT 1,
    [Remarks] NVARCHAR(MAX),

    -- ====================================================
    -- AUDIT & TRACKING
    -- ====================================================
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME2,
    [ModifiedBy] BIGINT,
    
    -- ====================================================
    -- INDEXES
    -- ====================================================
    INDEX IX_ReportCode NONCLUSTERED ([ReportCode]),
    INDEX IX_ReportCategory NONCLUSTERED ([ReportCategory]),
    INDEX IX_IsActive NONCLUSTERED ([IsActive]),
    INDEX IX_DisplayOrder NONCLUSTERED ([DisplayOrder])
);



INSERT INTO [dbo].[tblSystemReports] 
([ReportCode], [ReportName], [Remarks], [ReportCategory], [ReportType], 
 [ComponentPath], [IsActive], [DisplayOrder], [HasParameters], [SupportsPDF], 
 [SupportsExcel], [SupportsCSV], [SupportsEmail], [RequiredPermission], [MinimumUserRole], 
 [CreatedBy])
VALUES

-- ====================================================
-- ASSET REPORTS
-- ====================================================
('ASSET_REGISTER', 'Asset Register', 'Complete listing of all assets with values and depreciation', 
 'Asset', 'Register', 'Reports/AssetRegisterReport', 1, 10, 1, 1, 1, 1, 1, 
 'Reports.View.Assets', 'Viewer', 1),

('ASSET_DETAIL', 'Asset Detail Report', 'Detailed view of individual asset with full audit trail', 
 'Asset', 'Detail', 'Reports/AssetDetailReport', 1, 20, 1, 1, 1, 0, 0, 
 'Reports.View.Assets', 'Viewer', 1),

('DEPR_SCHEDULE', 'Depreciation Schedule', 'Complete depreciation schedule for accounting period', 
 'Asset', 'Schedule', 'Reports/DepreciationScheduleReport', 1, 30, 1, 1, 1, 1, 1, 
 'Reports.View.Assets', 'Analyst', 1),

('ASSET_MOVEMENT', 'Asset Movement Report', 'Track all additions, disposals, and transfers', 
 'Asset', 'List', 'Reports/AssetMovementReport', 1, 40, 1, 1, 1, 1, 0, 
 'Reports.View.Assets', 'Viewer', 1),

('ASSET_SUMMARY', 'Asset Summary', 'Summary totals by category and classification', 
 'Asset', 'Summary', 'Reports/AssetSummaryReport', 1, 50, 0, 1, 1, 0, 0, 
 'Reports.View.Assets', 'Viewer', 1),

('IMPAIRED_ASSETS', 'Impaired Assets Report', 'List of all impaired assets requiring attention', 
 'Asset', 'List', 'Reports/ImpairedAssetsReport', 1, 60, 0, 1, 1, 0, 1, 
 'Reports.View.Assets', 'Analyst', 1),

-- ====================================================
-- ASSESSMENT REPORTS
-- ====================================================
('ASSESSMENT_SUMMARY', 'Assessment Summary', 'Summary of assessment with key metrics', 
 'Assessment', 'Summary', 'Reports/AssessmentSummaryReport', 1, 10, 1, 1, 1, 0, 0, 
 'Reports.View.Assessments', 'Viewer', 1),

('ASSESSMENT_DETAIL', 'Assessment Detail', 'Complete assessment details and findings', 
 'Assessment', 'Detail', 'Reports/AssessmentDetailReport', 1, 20, 1, 1, 1, 1, 1, 
 'Reports.View.Assessments', 'Viewer', 1),

-- ====================================================
-- FINANCIAL REPORTS
-- ====================================================
('BALANCE_SHEET', 'Balance Sheet', 'Balance sheet presentation of assets', 
 'Financial', 'Register', 'Reports/BalanceSheetReport', 1, 10, 1, 1, 1, 0, 1, 
 'Reports.View.Financial', 'Analyst', 1),

('INCOME_STATEMENT', 'Income Statement', 'P&L with depreciation and asset movements', 
 'Financial', 'Summary', 'Reports/IncomeStatementReport', 1, 20, 1, 1, 1, 0, 1, 
 'Reports.View.Financial', 'Manager', 1),

('CASH_FLOW', 'Cash Flow Report', 'Cash flow impact of asset transactions', 
 'Financial', 'Summary', 'Reports/CashFlowReport', 1, 30, 1, 1, 1, 0, 0, 
 'Reports.View.Financial', 'Manager', 1),

-- ====================================================
-- COMPLIANCE & AUDIT REPORTS
-- ====================================================
('AUDIT_TRAIL', 'Audit Trail Report', 'Complete audit history of all asset changes', 
 'Compliance', 'Register', 'Reports/AuditTrailReport', 1, 10, 1, 1, 1, 1, 0, 
 'Reports.View.Compliance', 'Manager', 1),

('DATA_VALIDATION', 'Data Validation Report', 'Data quality and validation results', 
 'Compliance', 'Summary', 'Reports/DataValidationReport', 1, 20, 0, 1, 1, 0, 0, 
 'Reports.View.Compliance', 'Analyst', 1),

('POLICY_COMPLIANCE', 'Policy Compliance Report', 'Assets vs depreciation policy compliance', 
 'Compliance', 'Summary', 'Reports/PolicyComplianceReport', 1, 30, 0, 1, 1, 0, 0, 
 'Reports.View.Compliance', 'Manager', 1);