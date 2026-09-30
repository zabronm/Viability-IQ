SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.LeadSubmissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadSubmissions
    (
        LeadSubmissionId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_LeadSubmissions PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        PhoneNumber NVARCHAR(30) NULL,
        CompanyName NVARCHAR(150) NULL,
        Subject NVARCHAR(50) NOT NULL,
        Message NVARCHAR(2000) NOT NULL,
        Status NVARCHAR(30) NOT NULL
            CONSTRAINT DF_LeadSubmissions_Status DEFAULT N'New',
        SubmittedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_LeadSubmissions_SubmittedAtUtc DEFAULT SYSUTCDATETIME(),
        EmailSentAtUtc DATETIME2(0) NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.LeadSubmissions', N'PhoneNumber') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD PhoneNumber NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.LeadSubmissions', N'CompanyName') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD CompanyName NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.LeadSubmissions', N'Subject') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD Subject NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.LeadSubmissions', N'Message') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD Message NVARCHAR(2000) NULL;
IF COL_LENGTH(N'dbo.LeadSubmissions', N'Status') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD Status NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.LeadSubmissions', N'SubmittedAtUtc') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD SubmittedAtUtc DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.LeadSubmissions', N'EmailSentAtUtc') IS NULL
    ALTER TABLE dbo.LeadSubmissions ADD EmailSentAtUtc DATETIME2(0) NULL;
GO

EXEC sys.sp_executesql N'
    UPDATE dbo.LeadSubmissions
    SET Subject = COALESCE(NULLIF(Subject, N''''), N''Other''),
        Message = COALESCE(NULLIF(Message, N''''), N''Legacy website submission''),
        Status = COALESCE(NULLIF(Status, N''''), N''New''),
        SubmittedAtUtc = COALESCE(SubmittedAtUtc, SYSUTCDATETIME())
    WHERE Subject IS NULL OR Message IS NULL OR Status IS NULL OR SubmittedAtUtc IS NULL;';

ALTER TABLE dbo.LeadSubmissions ALTER COLUMN Subject NVARCHAR(50) NOT NULL;
ALTER TABLE dbo.LeadSubmissions ALTER COLUMN Message NVARCHAR(2000) NOT NULL;
ALTER TABLE dbo.LeadSubmissions ALTER COLUMN Status NVARCHAR(30) NOT NULL;
ALTER TABLE dbo.LeadSubmissions ALTER COLUMN SubmittedAtUtc DATETIME2(0) NOT NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.LeadSubmissions')
      AND c.name = N'Status'
)
    ALTER TABLE dbo.LeadSubmissions
        ADD CONSTRAINT DF_LeadSubmissions_Status DEFAULT N'New' FOR Status;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.LeadSubmissions')
      AND c.name = N'SubmittedAtUtc'
)
    ALTER TABLE dbo.LeadSubmissions
        ADD CONSTRAINT DF_LeadSubmissions_SubmittedAtUtc
        DEFAULT SYSUTCDATETIME() FOR SubmittedAtUtc;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.LeadSubmissions')
      AND name = N'IX_LeadSubmissions_SubmittedAtUtc'
)
    CREATE INDEX IX_LeadSubmissions_SubmittedAtUtc
        ON dbo.LeadSubmissions (SubmittedAtUtc DESC)
        INCLUDE (Status, Subject, Email);

COMMIT TRANSACTION;
