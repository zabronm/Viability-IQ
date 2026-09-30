SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.tblReportActivity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblReportActivity
    (
        ReportActivityId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblReportActivity PRIMARY KEY,
        ReportScope VARCHAR(20) NOT NULL,
        ReportCode VARCHAR(80) NOT NULL,
        ReportName NVARCHAR(160) NOT NULL,
        ReportVersion INT NOT NULL,
        AssessmentId BIGINT NULL,
        UserId NVARCHAR(128) NULL,
        ActivityType VARCHAR(30) NOT NULL,
        OutputFormat VARCHAR(20) NULL,
        RecipientMasked NVARCHAR(254) NULL,
        Succeeded BIT NOT NULL,
        ErrorCategory VARCHAR(50) NULL,
        CorrelationId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT CK_tblReportActivity_Scope
            CHECK (ReportScope IN ('Assessment', 'System')),
        CONSTRAINT CK_tblReportActivity_Activity
            CHECK (ActivityType IN ('Generated', 'PdfExported', 'ExcelExported', 'EmailSent', 'EmailFailed')),
        CONSTRAINT CK_tblReportActivity_Format
            CHECK (OutputFormat IS NULL OR OutputFormat IN ('Pdf', 'Excel', 'Html')),
        CONSTRAINT CK_tblReportActivity_AssessmentScope
            CHECK (ReportScope <> 'Assessment' OR AssessmentId IS NOT NULL)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblReportActivity_CreatedAtUtc'
    AND object_id = OBJECT_ID(N'dbo.tblReportActivity'))
    CREATE INDEX IX_tblReportActivity_CreatedAtUtc
        ON dbo.tblReportActivity (CreatedAtUtc DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblReportActivity_Assessment'
    AND object_id = OBJECT_ID(N'dbo.tblReportActivity'))
    CREATE INDEX IX_tblReportActivity_Assessment
        ON dbo.tblReportActivity (AssessmentId, CreatedAtUtc DESC)
        INCLUDE (ReportCode, ActivityType, Succeeded);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblReportActivity_Reporting'
    AND object_id = OBJECT_ID(N'dbo.tblReportActivity'))
    CREATE INDEX IX_tblReportActivity_Reporting
        ON dbo.tblReportActivity (ReportScope, ReportCode, ActivityType, Succeeded, CreatedAtUtc DESC);

COMMIT TRANSACTION;
