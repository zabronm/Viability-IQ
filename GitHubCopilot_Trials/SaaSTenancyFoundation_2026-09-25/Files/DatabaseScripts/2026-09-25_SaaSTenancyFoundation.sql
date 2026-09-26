SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.tblSubscriptionPlan', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSubscriptionPlan
    (
        SubscriptionPlanId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblSubscriptionPlan PRIMARY KEY,
        PlanCode NVARCHAR(40) NOT NULL,
        PlanName NVARCHAR(120) NOT NULL,
        TenantType NVARCHAR(30) NOT NULL,
        IncludedSeats INT NOT NULL,
        AllowsAdditionalSeats BIT NOT NULL
            CONSTRAINT DF_tblSubscriptionPlan_AllowsAdditionalSeats DEFAULT (0),
        TrialDays INT NOT NULL
            CONSTRAINT DF_tblSubscriptionPlan_TrialDays DEFAULT (14),
        Active BIT NOT NULL
            CONSTRAINT DF_tblSubscriptionPlan_Active DEFAULT (1),
        CreatedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblSubscriptionPlan_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_tblSubscriptionPlan_PlanCode UNIQUE (PlanCode),
        CONSTRAINT CK_tblSubscriptionPlan_Seats CHECK (IncludedSeats > 0),
        CONSTRAINT CK_tblSubscriptionPlan_TrialDays CHECK (TrialDays >= 0)
    );
END;

IF OBJECT_ID('dbo.tblTenant', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenant
    (
        TenantId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblTenant PRIMARY KEY,
        TenantType NVARCHAR(30) NOT NULL,
        TenantName NVARCHAR(200) NOT NULL,
        TenantSlug NVARCHAR(220) NOT NULL,
        OwnerUserId BIGINT NOT NULL,
        ProvinceId BIGINT NULL,
        Status NVARCHAR(30) NOT NULL,
        Active BIT NOT NULL
            CONSTRAINT DF_tblTenant_Active DEFAULT (1),
        CreatedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblTenant_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy BIGINT NOT NULL,
        ModifiedDate DATETIME2(0) NULL,
        ModifiedBy BIGINT NULL,
        CONSTRAINT UQ_tblTenant_TenantSlug UNIQUE (TenantSlug),
        CONSTRAINT CK_tblTenant_TenantType
            CHECK (TenantType IN ('Personal', 'Organisation'))
    );
END;

IF OBJECT_ID('dbo.tblTenantMembership', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenantMembership
    (
        TenantMembershipId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblTenantMembership PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        MembershipStatus NVARCHAR(30) NOT NULL,
        IsOwner BIT NOT NULL
            CONSTRAINT DF_tblTenantMembership_IsOwner DEFAULT (0),
        DefaultBranchId BIGINT NULL,
        JoinedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblTenantMembership_JoinedDate DEFAULT (SYSUTCDATETIME()),
        Active BIT NOT NULL
            CONSTRAINT DF_tblTenantMembership_Active DEFAULT (1),
        CONSTRAINT FK_tblTenantMembership_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId),
        CONSTRAINT UQ_tblTenantMembership_TenantUser UNIQUE (TenantId, UserId),
        CONSTRAINT CK_tblTenantMembership_Status
            CHECK (MembershipStatus IN ('Invited', 'Active', 'Suspended', 'Removed'))
    );
END;

IF OBJECT_ID('dbo.tblTenantRole', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenantRole
    (
        TenantRoleId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblTenantRole PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        RoleCode NVARCHAR(60) NOT NULL,
        RoleName NVARCHAR(120) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        IsProtected BIT NOT NULL
            CONSTRAINT DF_tblTenantRole_IsProtected DEFAULT (0),
        Active BIT NOT NULL
            CONSTRAINT DF_tblTenantRole_Active DEFAULT (1),
        CreatedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblTenantRole_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tblTenantRole_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId),
        CONSTRAINT UQ_tblTenantRole_TenantCode UNIQUE (TenantId, RoleCode)
    );
END;

IF OBJECT_ID('dbo.tblTenantMembershipRole', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenantMembershipRole
    (
        TenantMembershipId BIGINT NOT NULL,
        TenantRoleId BIGINT NOT NULL,
        AssignedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblTenantMembershipRole_AssignedDate DEFAULT (SYSUTCDATETIME()),
        AssignedBy BIGINT NOT NULL,
        CONSTRAINT PK_tblTenantMembershipRole
            PRIMARY KEY (TenantMembershipId, TenantRoleId),
        CONSTRAINT FK_tblTenantMembershipRole_Membership
            FOREIGN KEY (TenantMembershipId)
            REFERENCES dbo.tblTenantMembership(TenantMembershipId),
        CONSTRAINT FK_tblTenantMembershipRole_Role
            FOREIGN KEY (TenantRoleId)
            REFERENCES dbo.tblTenantRole(TenantRoleId)
    );
END;

IF OBJECT_ID('dbo.tblPlanEntitlement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblPlanEntitlement
    (
        PlanEntitlementId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblPlanEntitlement PRIMARY KEY,
        SubscriptionPlanId BIGINT NOT NULL,
        EntitlementKey NVARCHAR(120) NOT NULL,
        IsEnabled BIT NOT NULL,
        LimitValue INT NULL,
        CONSTRAINT FK_tblPlanEntitlement_Plan
            FOREIGN KEY (SubscriptionPlanId)
            REFERENCES dbo.tblSubscriptionPlan(SubscriptionPlanId),
        CONSTRAINT UQ_tblPlanEntitlement_PlanKey
            UNIQUE (SubscriptionPlanId, EntitlementKey)
    );
END;

IF OBJECT_ID('dbo.tblTenantSubscription', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenantSubscription
    (
        TenantSubscriptionId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblTenantSubscription PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        SubscriptionPlanId BIGINT NOT NULL,
        Provider NVARCHAR(60) NULL,
        ProviderCustomerId NVARCHAR(200) NULL,
        ProviderSubscriptionId NVARCHAR(200) NULL,
        Status NVARCHAR(30) NOT NULL,
        SeatQuantity INT NOT NULL,
        CurrentPeriodStart DATETIME2(0) NOT NULL,
        CurrentPeriodEnd DATETIME2(0) NULL,
        TrialEndsAt DATETIME2(0) NULL,
        CancelAtPeriodEnd BIT NOT NULL
            CONSTRAINT DF_tblTenantSubscription_CancelAtPeriodEnd DEFAULT (0),
        Active BIT NOT NULL
            CONSTRAINT DF_tblTenantSubscription_Active DEFAULT (1),
        CONSTRAINT FK_tblTenantSubscription_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId),
        CONSTRAINT FK_tblTenantSubscription_Plan
            FOREIGN KEY (SubscriptionPlanId)
            REFERENCES dbo.tblSubscriptionPlan(SubscriptionPlanId),
        CONSTRAINT CK_tblTenantSubscription_Seats CHECK (SeatQuantity > 0),
        CONSTRAINT CK_tblTenantSubscription_Status
            CHECK (Status IN ('Trial', 'Active', 'PastDue', 'Suspended', 'Cancelled'))
    );
END;

IF OBJECT_ID('dbo.tblTenantInvitation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblTenantInvitation
    (
        TenantInvitationId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblTenantInvitation PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        Email NVARCHAR(320) NOT NULL,
        RoleCode NVARCHAR(60) NOT NULL,
        BranchId BIGINT NULL,
        TokenHash NVARCHAR(200) NOT NULL,
        ExpiresAt DATETIME2(0) NOT NULL,
        AcceptedAt DATETIME2(0) NULL,
        InvitedBy BIGINT NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        Active BIT NOT NULL
            CONSTRAINT DF_tblTenantInvitation_Active DEFAULT (1),
        CreatedDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_tblTenantInvitation_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tblTenantInvitation_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId)
    );
    CREATE INDEX IX_tblTenantInvitation_TenantEmail
        ON dbo.tblTenantInvitation(TenantId, Email, Active);
END;

MERGE dbo.tblSubscriptionPlan AS target
USING
(
    VALUES
        ('STANDARD', 'Standard', 'Personal', 1, 0, 0),
        ('OPEN', 'Open Plan', 'Organisation', 2, 1, 0)
) AS source
    (PlanCode, PlanName, TenantType, IncludedSeats, AllowsAdditionalSeats, TrialDays)
ON target.PlanCode = source.PlanCode
WHEN MATCHED THEN
    UPDATE SET
        PlanName = source.PlanName,
        TenantType = source.TenantType,
        IncludedSeats = source.IncludedSeats,
        AllowsAdditionalSeats = source.AllowsAdditionalSeats,
        TrialDays = source.TrialDays,
        Active = 1
WHEN NOT MATCHED THEN
    INSERT
        (PlanCode, PlanName, TenantType, IncludedSeats,
         AllowsAdditionalSeats, TrialDays, Active)
    VALUES
        (source.PlanCode, source.PlanName, source.TenantType, source.IncludedSeats,
         source.AllowsAdditionalSeats, source.TrialDays, 1);

DECLARE @StandardPlanId BIGINT =
(
    SELECT SubscriptionPlanId
    FROM dbo.tblSubscriptionPlan
    WHERE PlanCode = 'STANDARD'
);

DECLARE @OpenPlanId BIGINT =
(
    SELECT SubscriptionPlanId
    FROM dbo.tblSubscriptionPlan
    WHERE PlanCode = 'OPEN'
);

MERGE dbo.tblPlanEntitlement AS target
USING
(
    VALUES
        (@StandardPlanId, 'users.max_seats', 1, 1),
        (@StandardPlanId, 'branches.enabled', 0, 0),
        (@StandardPlanId, 'users.invite', 0, 0),
        (@StandardPlanId, 'roles.manage', 0, 0),
        (@OpenPlanId, 'users.max_seats', 1, NULL),
        (@OpenPlanId, 'branches.enabled', 1, NULL),
        (@OpenPlanId, 'users.invite', 1, NULL),
        (@OpenPlanId, 'roles.manage', 1, NULL)
) AS source
    (SubscriptionPlanId, EntitlementKey, IsEnabled, LimitValue)
ON target.SubscriptionPlanId = source.SubscriptionPlanId
AND target.EntitlementKey = source.EntitlementKey
WHEN MATCHED THEN
    UPDATE SET
        IsEnabled = source.IsEnabled,
        LimitValue = source.LimitValue
WHEN NOT MATCHED THEN
    INSERT (SubscriptionPlanId, EntitlementKey, IsEnabled, LimitValue)
    VALUES
        (source.SubscriptionPlanId, source.EntitlementKey,
         source.IsEnabled, source.LimitValue);

IF OBJECT_ID('dbo.tblApplicationUsers', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.tblTenant
        (TenantType, TenantName, TenantSlug, OwnerUserId, ProvinceId,
         Status, Active, CreatedDate, CreatedBy)
    SELECT
        'Personal',
        COALESCE(
            NULLIF(LTRIM(RTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''),
            u.Email,
            CONCAT('User ', u.UserId)),
        CONCAT('personal-', u.UserId),
        u.UserId,
        u.ProvinceId,
        'Active',
        1,
        SYSUTCDATETIME(),
        u.UserId
    FROM dbo.tblApplicationUsers u
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tblTenantMembership m
        WHERE m.UserId = u.UserId AND m.Active = 1
    );

    INSERT INTO dbo.tblTenantMembership
        (TenantId, UserId, MembershipStatus, IsOwner, JoinedDate, Active)
    SELECT
        t.TenantId,
        t.OwnerUserId,
        'Active',
        1,
        SYSUTCDATETIME(),
        1
    FROM dbo.tblTenant t
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tblTenantMembership m
        WHERE m.TenantId = t.TenantId AND m.UserId = t.OwnerUserId
    );

    INSERT INTO dbo.tblTenantSubscription
        (TenantId, SubscriptionPlanId, Status, SeatQuantity,
         CurrentPeriodStart, TrialEndsAt, CancelAtPeriodEnd, Active)
    SELECT
        t.TenantId,
        @StandardPlanId,
        'Active',
        1,
        SYSUTCDATETIME(),
        NULL,
        0,
        1
    FROM dbo.tblTenant t
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tblTenantSubscription s
        WHERE s.TenantId = t.TenantId AND s.Active = 1
    );

    ;WITH RoleTemplates AS
    (
        SELECT *
        FROM
        (
            VALUES
                ('TENANT_OWNER', 'Tenant Owner',
                 'Owns the tenant and controls all tenant settings.'),
                ('TENANT_ADMIN', 'Tenant Administrator',
                 'Manages tenant users, setup, and operational records.'),
                ('BRANCH_MANAGER', 'Branch Manager',
                 'Manages a branch and performs assessment review responsibilities.'),
                ('VIEWER', 'Viewer',
                 'Views authorised tenant records.'),
                ('BILLING_MANAGER', 'Billing Manager',
                 'Manages subscription and billing settings.')
        ) roles(RoleCode, RoleName, Description)
    )
    INSERT INTO dbo.tblTenantRole
        (TenantId, RoleCode, RoleName, Description, IsProtected, Active, CreatedDate)
    SELECT
        t.TenantId,
        r.RoleCode,
        r.RoleName,
        r.Description,
        1,
        1,
        SYSUTCDATETIME()
    FROM dbo.tblTenant t
    CROSS JOIN RoleTemplates r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tblTenantRole tr
        WHERE tr.TenantId = t.TenantId AND tr.RoleCode = r.RoleCode
    );

    INSERT INTO dbo.tblTenantMembershipRole
        (TenantMembershipId, TenantRoleId, AssignedDate, AssignedBy)
    SELECT
        m.TenantMembershipId,
        r.TenantRoleId,
        SYSUTCDATETIME(),
        m.UserId
    FROM dbo.tblTenantMembership m
    INNER JOIN dbo.tblTenantRole r
        ON r.TenantId = m.TenantId
       AND r.RoleCode = 'TENANT_OWNER'
    WHERE m.IsOwner = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.tblTenantMembershipRole mr
          WHERE mr.TenantMembershipId = m.TenantMembershipId
            AND mr.TenantRoleId = r.TenantRoleId
      );
END;

IF COL_LENGTH('dbo.tblClient', 'TenantId') IS NULL
    ALTER TABLE dbo.tblClient ADD TenantId BIGINT NULL;

IF COL_LENGTH('dbo.tblBusiness', 'TenantId') IS NULL
    ALTER TABLE dbo.tblBusiness ADD TenantId BIGINT NULL;

IF COL_LENGTH('dbo.tblAssessments', 'TenantId') IS NULL
    ALTER TABLE dbo.tblAssessments ADD TenantId BIGINT NULL;

IF COL_LENGTH('dbo.tblCompany', 'TenantId') IS NULL
    ALTER TABLE dbo.tblCompany ADD TenantId BIGINT NULL;

IF COL_LENGTH('dbo.tblBranch', 'TenantId') IS NULL
    ALTER TABLE dbo.tblBranch ADD TenantId BIGINT NULL;

-- Use deferred compilation because these columns may have been added earlier
-- in this same batch. Static statements would be compiled before ALTER TABLE.
EXEC sys.sp_executesql N'
    UPDATE record
    SET TenantId =
    (
        SELECT MIN(m.TenantId)
        FROM dbo.tblTenantMembership m
        WHERE m.UserId = record.CreatedBy AND m.Active = 1
    )
    FROM dbo.tblClient record
    WHERE record.TenantId IS NULL;';

EXEC sys.sp_executesql N'
    UPDATE record
    SET TenantId =
    (
        SELECT MIN(m.TenantId)
        FROM dbo.tblTenantMembership m
        WHERE m.UserId = record.CreatedBy AND m.Active = 1
    )
    FROM dbo.tblBusiness record
    WHERE record.TenantId IS NULL;';

EXEC sys.sp_executesql N'
    UPDATE record
    SET TenantId =
    (
        SELECT MIN(m.TenantId)
        FROM dbo.tblTenantMembership m
        WHERE m.UserId = record.CreatedBy AND m.Active = 1
    )
    FROM dbo.tblAssessments record
    WHERE record.TenantId IS NULL;';

EXEC sys.sp_executesql N'
    UPDATE record
    SET TenantId =
    (
        SELECT MIN(m.TenantId)
        FROM dbo.tblTenantMembership m
        WHERE m.UserId = record.CreatedBy AND m.Active = 1
    )
    FROM dbo.tblCompany record
    WHERE record.TenantId IS NULL;';

EXEC sys.sp_executesql N'
    UPDATE record
    SET TenantId =
    (
        SELECT MIN(m.TenantId)
        FROM dbo.tblTenantMembership m
        WHERE m.UserId = record.CreatedBy AND m.Active = 1
    )
    FROM dbo.tblBranch record
    WHERE record.TenantId IS NULL;';

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_tblClient_TenantId_Active'
      AND object_id = OBJECT_ID('dbo.tblClient')
)
    EXEC sys.sp_executesql N'
        CREATE INDEX IX_tblClient_TenantId_Active
            ON dbo.tblClient(TenantId, Active);';

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_tblBusiness_TenantId_Active'
      AND object_id = OBJECT_ID('dbo.tblBusiness')
)
    EXEC sys.sp_executesql N'
        CREATE INDEX IX_tblBusiness_TenantId_Active
            ON dbo.tblBusiness(TenantId, Active);';

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_tblAssessments_TenantId_Active'
      AND object_id = OBJECT_ID('dbo.tblAssessments')
)
    EXEC sys.sp_executesql N'
        CREATE INDEX IX_tblAssessments_TenantId_Active
            ON dbo.tblAssessments(TenantId, Active);';

COMMIT TRANSACTION;
