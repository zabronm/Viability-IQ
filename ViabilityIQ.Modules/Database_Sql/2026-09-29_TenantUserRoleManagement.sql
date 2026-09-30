SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.tblApplicationUsers', 'MustChangePassword') IS NULL
BEGIN
    ALTER TABLE dbo.tblApplicationUsers
        ADD MustChangePassword bit NOT NULL
            CONSTRAINT DF_tblApplicationUsers_MustChangePassword DEFAULT (0);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.tblTenantMembership')
      AND name = 'IX_tblTenantMembership_TenantStatus'
)
BEGIN
    CREATE INDEX IX_tblTenantMembership_TenantStatus
        ON dbo.tblTenantMembership (TenantId, MembershipStatus, Active)
        INCLUDE (UserId, DefaultBranchId, IsOwner);
END;

COMMIT TRANSACTION;
