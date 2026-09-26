SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SystemAnnouncements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemAnnouncements
    (
        AnnouncementId int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SystemAnnouncements PRIMARY KEY,
        Title nvarchar(200) NOT NULL,
        Message nvarchar(max) NOT NULL,
        AnnouncementType nvarchar(50) NOT NULL
            CONSTRAINT DF_SystemAnnouncements_AnnouncementType DEFAULT ('Info'),
        CreatedDate datetime2 NOT NULL
            CONSTRAINT DF_SystemAnnouncements_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ExpiryDate datetime2 NULL,
        IsActive bit NOT NULL
            CONSTRAINT DF_SystemAnnouncements_IsActive DEFAULT (1)
    );

    CREATE INDEX IX_SystemAnnouncements_ActiveExpiry
        ON dbo.SystemAnnouncements (IsActive, ExpiryDate, CreatedDate DESC);
END;

IF OBJECT_ID(N'dbo.UserAnnouncementDismissals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAnnouncementDismissals
    (
        DismissalId bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_UserAnnouncementDismissals PRIMARY KEY,
        UserId bigint NOT NULL,
        AnnouncementId int NOT NULL,
        DismissalDate datetime2 NOT NULL
            CONSTRAINT DF_UserAnnouncementDismissals_DismissalDate DEFAULT (SYSUTCDATETIME()),
        DismissalExpiry datetime2 NOT NULL,
        CONSTRAINT FK_UserAnnouncementDismissals_Announcement
            FOREIGN KEY (AnnouncementId)
            REFERENCES dbo.SystemAnnouncements (AnnouncementId)
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX UX_UserAnnouncementDismissals_UserAnnouncement
        ON dbo.UserAnnouncementDismissals (UserId, AnnouncementId);
    CREATE INDEX IX_UserAnnouncementDismissals_Expiry
        ON dbo.UserAnnouncementDismissals (DismissalExpiry);
END;

IF OBJECT_ID(N'dbo.UserAlertDismissals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAlertDismissals
    (
        DismissalId bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_UserAlertDismissals PRIMARY KEY,
        UserId bigint NOT NULL,
        AlertId nvarchar(100) NOT NULL,
        AlertType nvarchar(50) NOT NULL,
        DismissalDate datetime2 NOT NULL
            CONSTRAINT DF_UserAlertDismissals_DismissalDate DEFAULT (SYSUTCDATETIME()),
        DismissalExpiry datetime2 NOT NULL
    );

    CREATE UNIQUE INDEX UX_UserAlertDismissals_UserAlert
        ON dbo.UserAlertDismissals (UserId, AlertId, AlertType);
    CREATE INDEX IX_UserAlertDismissals_Expiry
        ON dbo.UserAlertDismissals (DismissalExpiry);
END;

COMMIT TRANSACTION;
