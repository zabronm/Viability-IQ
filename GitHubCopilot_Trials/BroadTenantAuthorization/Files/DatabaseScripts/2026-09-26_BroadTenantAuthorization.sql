SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.tblTenantRecordGrant', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenantRecordGrant
    (
        TenantRecordGrantId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblTenantRecordGrant PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        TenantMembershipId BIGINT NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId BIGINT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_tblTenantRecordGrant_CanView DEFAULT (1),
        CanEdit BIT NOT NULL CONSTRAINT DF_tblTenantRecordGrant_CanEdit DEFAULT (0),
        CanDelete BIT NOT NULL CONSTRAINT DF_tblTenantRecordGrant_CanDelete DEFAULT (0),
        Active BIT NOT NULL CONSTRAINT DF_tblTenantRecordGrant_Active DEFAULT (1),
        GrantedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblTenantRecordGrant_GrantedDate DEFAULT (SYSUTCDATETIME()),
        GrantedBy BIGINT NOT NULL,
        CONSTRAINT FK_tblTenantRecordGrant_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId),
        CONSTRAINT FK_tblTenantRecordGrant_Membership
            FOREIGN KEY (TenantMembershipId)
            REFERENCES dbo.tblTenantMembership(TenantMembershipId),
        CONSTRAINT CK_tblTenantRecordGrant_EntityType
            CHECK (EntityType IN ('Assessment', 'Business', 'Client', 'Company', 'Branch')),
        CONSTRAINT UQ_tblTenantRecordGrant_Record
            UNIQUE (TenantId, TenantMembershipId, EntityType, EntityId)
    );

    CREATE INDEX IX_tblTenantRecordGrant_Access
        ON dbo.tblTenantRecordGrant
            (TenantId, TenantMembershipId, EntityType, EntityId, Active)
        INCLUDE (CanView, CanEdit, CanDelete);
END;

COMMIT TRANSACTION;

