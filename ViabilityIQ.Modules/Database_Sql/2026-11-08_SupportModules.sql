SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SupportConversations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportConversations
    (
        SupportConversationId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SupportConversations PRIMARY KEY,
        UserId BIGINT NOT NULL,
        TenantId BIGINT NOT NULL,
        UserName NVARCHAR(160) NOT NULL,
        UserEmail NVARCHAR(254) NOT NULL,
        ConsultantUserId BIGINT NULL,
        ConsultantName NVARCHAR(160) NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_SupportConversations_Status DEFAULT N'Waiting',
        CreatedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_SupportConversations_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        LastUserActivityAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_SupportConversations_LastUserActivityAtUtc DEFAULT SYSUTCDATETIME(),
        ClosedAtUtc DATETIME2(0) NULL,
        LastMessage NVARCHAR(4000) NULL,
        LastMessageAtUtc DATETIME2(0) NULL,
        CONSTRAINT CK_SupportConversations_Status
            CHECK (Status IN (N'Waiting', N'Active', N'Closed', N'Expired'))
    );
END;

IF OBJECT_ID(N'dbo.SupportTickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTickets
    (
        SupportTicketId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SupportTickets PRIMARY KEY,
        UserId BIGINT NOT NULL,
        TenantId BIGINT NOT NULL,
        UserName NVARCHAR(160) NOT NULL,
        UserEmail NVARCHAR(254) NOT NULL,
        Title NVARCHAR(160) NOT NULL,
        Message NVARCHAR(10000) NOT NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_SupportTickets_Status DEFAULT N'Open',
        CreatedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_SupportTickets_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        EmailSentAtUtc DATETIME2(0) NULL,
        CONSTRAINT CK_SupportTickets_Status
            CHECK (Status IN (N'Open', N'In Progress', N'Closed'))
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportConversations')
      AND name = N'IX_SupportConversations_Status_LastMessage'
)
    CREATE INDEX IX_SupportConversations_Status_LastMessage
        ON dbo.SupportConversations (Status, LastMessageAtUtc DESC)
        INCLUDE (UserId, TenantId, UserName, ConsultantUserId, LastUserActivityAtUtc);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportTickets')
      AND name = N'IX_SupportTickets_CreatedAtUtc'
)
    CREATE INDEX IX_SupportTickets_CreatedAtUtc
        ON dbo.SupportTickets (CreatedAtUtc DESC)
        INCLUDE (UserId, TenantId, Title, Status, EmailSentAtUtc);

COMMIT TRANSACTION;
