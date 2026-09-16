CREATE TABLE dbo.tblActivityLog
(
    ActivityLogId bigint IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_tblActivityLog PRIMARY KEY,
    UserId bigint NULL,
    ActorName nvarchar(200) NULL,
    ActivityAction nvarchar(50) NOT NULL,
    EntityType nvarchar(100) NOT NULL,
    EntityId bigint NULL,
    EntityName nvarchar(300) NULL,
    AssessmentId bigint NULL,
    AssessmentName nvarchar(300) NULL,
    Module nvarchar(100) NULL,
    Page nvarchar(500) NULL,
    IpAddress nvarchar(100) NULL,
    UserAgent nvarchar(1000) NULL,
    CorrelationId nvarchar(100) NULL,
    MetadataJson nvarchar(max) NULL,
    Remarks nvarchar(1000) NULL,
    Active bit NOT NULL
        CONSTRAINT DF_tblActivityLog_Active DEFAULT (1),
    CreatedDate datetime2 NOT NULL
        CONSTRAINT DF_tblActivityLog_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy bigint NOT NULL
        CONSTRAINT DF_tblActivityLog_CreatedBy DEFAULT (0),
    ModifiedDate datetime2 NOT NULL
        CONSTRAINT DF_tblActivityLog_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy bigint NOT NULL
        CONSTRAINT DF_tblActivityLog_ModifiedBy DEFAULT (0)
);

CREATE INDEX IX_tblActivityLog_CreatedDate
    ON dbo.tblActivityLog (CreatedDate DESC);

CREATE INDEX IX_tblActivityLog_UserId_CreatedDate
    ON dbo.tblActivityLog (UserId, CreatedDate DESC);

CREATE INDEX IX_tblActivityLog_AssessmentId_CreatedDate
    ON dbo.tblActivityLog (AssessmentId, CreatedDate DESC);

CREATE INDEX IX_tblActivityLog_Entity
    ON dbo.tblActivityLog (EntityType, EntityId);

CREATE INDEX IX_tblActivityLog_UserFilters
    ON dbo.tblActivityLog (UserId, ActivityAction, EntityType, CreatedDate DESC)
    INCLUDE (ActorName, EntityName, AssessmentName, Module, Active);
