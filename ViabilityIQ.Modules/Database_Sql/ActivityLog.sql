CREATE TABLE dbo.tblActivityLog
(
    ActivityLogId BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_tblActivityLog PRIMARY KEY,

    UserId BIGINT NULL,
    ActorName NVARCHAR(200) NULL,
    ActivityAction NVARCHAR(50) NOT NULL,
    EntityType NVARCHAR(100) NOT NULL,
    EntityId BIGINT NULL,
    EntityName NVARCHAR(300) NULL,
    AssessmentId BIGINT NULL,
    AssessmentName NVARCHAR(300) NULL,
    Remarks NVARCHAR(1000) NULL,
    Active BIT,
    Module NVARCHAR(100) NULL,
    Page NVARCHAR(500) NULL,
    IpAddress NVARCHAR(100) NULL,
    UserAgent NVARCHAR(1000) NULL,
    CorrelationId NVARCHAR(100) NULL,
    MetadataJson NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL
        CONSTRAINT DF_tblActivityLog_CreatedDate
        DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2 NOT NULL        
        DEFAULT SYSUTCDATETIME(),
    CreatedBy BIGINT NULL,
    ModifiedBy BIGINT NULL,
);




CREATE INDEX IX_tblActivityLog_CreatedDate
ON dbo.tblActivityLog (CreatedDate DESC);

CREATE INDEX IX_tblActivityLog_UserId_CreatedDate
ON dbo.tblActivityLog (UserId, CreatedDate DESC);

CREATE INDEX IX_tblActivityLog_AssessmentId_CreatedDate
ON dbo.tblActivityLog (AssessmentId, CreatedDate DESC);

CREATE INDEX IX_tblActivityLog_Entity
ON dbo.tblActivityLog (EntityType, EntityId);