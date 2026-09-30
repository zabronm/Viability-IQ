SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.tblApplicationUsers', 'PreviousLoginAt') IS NULL
BEGIN
    ALTER TABLE dbo.tblApplicationUsers
        ADD PreviousLoginAt DATETIME2(7) NULL;
END;

IF COL_LENGTH('dbo.tblApplicationUsers', 'LastLogoutAt') IS NULL
BEGIN
    ALTER TABLE dbo.tblApplicationUsers
        ADD LastLogoutAt DATETIME2(7) NULL;
END;

COMMIT TRANSACTION;
