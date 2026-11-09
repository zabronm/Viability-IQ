SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.tblNotificationItem', N'U') IS NULL
    THROW 50000, 'The existing messaging table dbo.tblNotificationItem is required before integrating support chat.', 1;

IF OBJECT_ID(N'dbo.SupportConversations', N'U') IS NULL
    THROW 50000, 'Apply 2026-11-08_SupportModules.sql before integrating support chat.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.tblNotificationItem', N'SupportConversationId') IS NULL
    ALTER TABLE dbo.tblNotificationItem ADD SupportConversationId BIGINT NULL;

IF COL_LENGTH(N'dbo.tblNotificationItem', N'SupportSenderName') IS NULL
    ALTER TABLE dbo.tblNotificationItem ADD SupportSenderName NVARCHAR(160) NULL;

IF COL_LENGTH(N'dbo.tblNotificationItem', N'IsSupportMessage') IS NULL
    ALTER TABLE dbo.tblNotificationItem
        ADD IsSupportMessage BIT NOT NULL
            CONSTRAINT DF_tblNotificationItem_IsSupportMessage DEFAULT (0) WITH VALUES;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.tblNotificationItem')
      AND name = N'FK_tblNotificationItem_SupportConversations'
)
    EXEC(N'ALTER TABLE dbo.tblNotificationItem
        ADD CONSTRAINT FK_tblNotificationItem_SupportConversations
            FOREIGN KEY (SupportConversationId)
            REFERENCES dbo.SupportConversations (SupportConversationId);');

IF OBJECT_ID(N'dbo.SupportMessages', N'U') IS NOT NULL
    EXEC(N'
        INSERT INTO dbo.tblNotificationItem
            (SenderId, RecipientId, Title, Message, IsRead, IsTask, TaskStatus,
             CreatedDate, Active, CreatedBy, SupportConversationId,
             SupportSenderName, IsSupportMessage)
        SELECT sm.SenderUserId,
               CASE WHEN sm.IsConsultant = 1 THEN c.UserId
                    ELSE COALESCE(c.ConsultantUserId, c.UserId) END,
               N''Support chat'',
               sm.MessageText,
               1,
               0,
               0,
               sm.SentAtUtc,
               1,
               sm.SenderUserId,
               sm.SupportConversationId,
               sm.SenderName,
               1
        FROM dbo.SupportMessages sm
        INNER JOIN dbo.SupportConversations c
            ON c.SupportConversationId = sm.SupportConversationId
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.tblNotificationItem existing
            WHERE existing.SupportConversationId = sm.SupportConversationId
              AND existing.SenderId = sm.SenderUserId
              AND existing.SupportSenderName = sm.SenderName
              AND existing.Message = sm.MessageText
              AND existing.CreatedDate = sm.SentAtUtc
              AND existing.IsSupportMessage = 1
        );');

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.tblNotificationItem')
      AND name = N'IX_tblNotificationItem_SupportConversation'
)
    EXEC(N'
        CREATE INDEX IX_tblNotificationItem_SupportConversation
            ON dbo.tblNotificationItem (SupportConversationId, CreatedDate, NotificationItemId)
            INCLUDE (SenderId, RecipientId, IsRead, SupportSenderName)
            WHERE IsSupportMessage = 1;');

COMMIT TRANSACTION;
