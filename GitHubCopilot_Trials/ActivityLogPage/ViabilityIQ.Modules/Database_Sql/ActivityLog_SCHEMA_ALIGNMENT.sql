/*
Run this script only after taking a database backup.

It aligns an existing tblActivityLog table with the C# ActivityLog entity.
ActivityAction is the canonical action field. ActivityType is intentionally
not used because it duplicates ActivityAction and was absent from the table.
*/

IF OBJECT_ID(N'dbo.tblActivityLog', N'U') IS NULL
BEGIN
    THROW 50001, 'dbo.tblActivityLog does not exist. Run ActivityLog.sql first.', 1;
END;

UPDATE dbo.tblActivityLog
SET Active = 1
WHERE Active IS NULL;

UPDATE dbo.tblActivityLog
SET CreatedBy = COALESCE(CreatedBy, UserId, 0),
    ModifiedBy = COALESCE(ModifiedBy, CreatedBy, UserId, 0)
WHERE CreatedBy IS NULL
   OR ModifiedBy IS NULL;

ALTER TABLE dbo.tblActivityLog ALTER COLUMN Active bit NOT NULL;
ALTER TABLE dbo.tblActivityLog ALTER COLUMN CreatedBy bigint NOT NULL;
ALTER TABLE dbo.tblActivityLog ALTER COLUMN ModifiedBy bigint NOT NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tblActivityLog')
      AND c.name = N'Active'
)
BEGIN
    ALTER TABLE dbo.tblActivityLog
        ADD CONSTRAINT DF_tblActivityLog_Active DEFAULT (1) FOR Active;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tblActivityLog')
      AND c.name = N'CreatedBy'
)
BEGIN
    ALTER TABLE dbo.tblActivityLog
        ADD CONSTRAINT DF_tblActivityLog_CreatedBy DEFAULT (0) FOR CreatedBy;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tblActivityLog')
      AND c.name = N'ModifiedBy'
)
BEGIN
    ALTER TABLE dbo.tblActivityLog
        ADD CONSTRAINT DF_tblActivityLog_ModifiedBy DEFAULT (0) FOR ModifiedBy;
END;
