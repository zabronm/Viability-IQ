SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.tblBusiness', 'BusinessCode') IS NULL
        ALTER TABLE dbo.tblBusiness ADD BusinessCode varchar(12) NULL;

    IF COL_LENGTH('dbo.tblBranch', 'BranchCode') IS NULL
        ALTER TABLE dbo.tblBranch ADD BranchCode varchar(12) NULL;

    IF COL_LENGTH('dbo.tblAssessments', 'BranchId') IS NULL
        ALTER TABLE dbo.tblAssessments ADD BranchId bigint NULL;

    IF COL_LENGTH('dbo.tblAssessments', 'CaseYear') IS NULL
        ALTER TABLE dbo.tblAssessments ADD CaseYear int NULL;

    IF COL_LENGTH('dbo.tblAssessments', 'CaseSequence') IS NULL
        ALTER TABLE dbo.tblAssessments ADD CaseSequence int NULL;

    IF OBJECT_ID('dbo.tblAssessmentCaseSequence', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tblAssessmentCaseSequence
        (
            BusinessId bigint NOT NULL,
            BranchId bigint NOT NULL,
            CaseYear int NOT NULL,
            LastSequence int NOT NULL,
            ModifiedDate datetime2 NOT NULL
                CONSTRAINT DF_tblAssessmentCaseSequence_ModifiedDate
                DEFAULT SYSUTCDATETIME(),
            CONSTRAINT PK_tblAssessmentCaseSequence
                PRIMARY KEY (BusinessId, BranchId, CaseYear),
            CONSTRAINT FK_tblAssessmentCaseSequence_Business
                FOREIGN KEY (BusinessId) REFERENCES dbo.tblBusiness(BusinessId),
            CONSTRAINT FK_tblAssessmentCaseSequence_Branch
                FOREIGN KEY (BranchId) REFERENCES dbo.tblBranch(BranchId),
            CONSTRAINT CK_tblAssessmentCaseSequence_Positive
                CHECK (LastSequence > 0)
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE
        @Id bigint,
        @Name nvarchar(500),
        @Normalized varchar(200),
        @Character nchar(1),
        @Position int,
        @BaseCode varchar(3),
        @Candidate varchar(12),
        @Suffix int;

    DECLARE business_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT BusinessId, BusinessName
        FROM dbo.tblBusiness
        WHERE NULLIF(LTRIM(RTRIM(BusinessCode)), '') IS NULL
        ORDER BY BusinessId;

    OPEN business_cursor;
    FETCH NEXT FROM business_cursor INTO @Id, @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Normalized = '';
        SET @Position = 1;
        WHILE @Position <= LEN(COALESCE(@Name, ''))
        BEGIN
            SET @Character = UPPER(SUBSTRING(@Name, @Position, 1));
            IF @Character LIKE '[A-Z0-9]'
                SET @Normalized += @Character;
            SET @Position += 1;
        END;

        SET @Normalized = COALESCE(NULLIF(@Normalized, ''), 'BUS');
        SET @BaseCode = LEFT(@Normalized + 'BUS', 3);
        SET @Candidate = @BaseCode;
        SET @Suffix = 0;
        WHILE EXISTS
        (
            SELECT 1
            FROM dbo.tblBusiness
            WHERE BusinessCode = @Candidate
              AND BusinessId <> @Id
        )
        BEGIN
            SET @Candidate =
                LEFT(@BaseCode, 12 - LEN(CONVERT(varchar(10), @Suffix)))
                + CONVERT(varchar(10), @Suffix);
            SET @Suffix += 1;
        END;

        UPDATE dbo.tblBusiness
        SET BusinessCode = @Candidate
        WHERE BusinessId = @Id;

        FETCH NEXT FROM business_cursor INTO @Id, @Name;
    END;
    CLOSE business_cursor;
    DEALLOCATE business_cursor;

    DECLARE @TenantId bigint;
    DECLARE branch_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT BranchId, TenantId, BranchName
        FROM dbo.tblBranch
        WHERE NULLIF(LTRIM(RTRIM(BranchCode)), '') IS NULL
        ORDER BY TenantId, BranchId;

    OPEN branch_cursor;
    FETCH NEXT FROM branch_cursor INTO @Id, @TenantId, @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Normalized = '';
        SET @Position = 1;
        WHILE @Position <= LEN(COALESCE(@Name, ''))
        BEGIN
            SET @Character = UPPER(SUBSTRING(@Name, @Position, 1));
            IF @Character LIKE '[A-Z0-9]'
                SET @Normalized += @Character;
            SET @Position += 1;
        END;

        SET @Normalized = COALESCE(NULLIF(@Normalized, ''), 'SIT');
        SET @BaseCode = LEFT(@Normalized + 'SIT', 3);
        SET @Candidate = @BaseCode;
        SET @Suffix = 0;
        WHILE EXISTS
        (
            SELECT 1
            FROM dbo.tblBranch
            WHERE BranchCode = @Candidate
              AND ISNULL(TenantId, -1) = ISNULL(@TenantId, -1)
              AND BranchId <> @Id
        )
        BEGIN
            SET @Candidate =
                LEFT(@BaseCode, 12 - LEN(CONVERT(varchar(10), @Suffix)))
                + CONVERT(varchar(10), @Suffix);
            SET @Suffix += 1;
        END;

        UPDATE dbo.tblBranch
        SET BranchCode = @Candidate
        WHERE BranchId = @Id;

        FETCH NEXT FROM branch_cursor INTO @Id, @TenantId, @Name;
    END;
    CLOSE branch_cursor;
    DEALLOCATE branch_cursor;

    UPDATE assessment
    SET BranchId = membership.BranchId
    FROM dbo.tblAssessments assessment
    OUTER APPLY
    (
        SELECT TOP (1) tenantMembership.DefaultBranchId AS BranchId
        FROM dbo.tblTenantMembership tenantMembership
        WHERE tenantMembership.TenantId = assessment.TenantId
          AND tenantMembership.UserId = assessment.CreatedBy
          AND tenantMembership.MembershipStatus <> 'Removed'
          AND tenantMembership.DefaultBranchId IS NOT NULL
        ORDER BY tenantMembership.IsOwner DESC, tenantMembership.TenantMembershipId
    ) membership
    WHERE assessment.BranchId IS NULL
      AND membership.BranchId IS NOT NULL;

    UPDATE assessment
    SET BranchId = branch.BranchId
    FROM dbo.tblAssessments assessment
    CROSS APPLY
    (
        SELECT TOP (1) tenantBranch.BranchId
        FROM dbo.tblBranch tenantBranch
        WHERE tenantBranch.TenantId = assessment.TenantId
          AND tenantBranch.Active = 1
        ORDER BY tenantBranch.SortOrder, tenantBranch.BranchId
    ) branch
    WHERE assessment.BranchId IS NULL;

    ALTER TABLE dbo.tblBusiness
        ALTER COLUMN BusinessCode varchar(12) NOT NULL;

    ALTER TABLE dbo.tblBranch
        ALTER COLUMN BranchCode varchar(12) NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblBusiness')
          AND name = 'UX_tblBusiness_BusinessCode'
    )
        CREATE UNIQUE INDEX UX_tblBusiness_BusinessCode
            ON dbo.tblBusiness(BusinessCode)
            WHERE BusinessCode IS NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblBranch')
          AND name = 'UX_tblBranch_Tenant_BranchCode'
    )
        CREATE UNIQUE INDEX UX_tblBranch_Tenant_BranchCode
            ON dbo.tblBranch(TenantId, BranchCode)
            WHERE BranchCode IS NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblAssessments')
          AND name = 'UX_tblAssessments_SiteCaseSequence'
    )
        CREATE UNIQUE INDEX UX_tblAssessments_SiteCaseSequence
            ON dbo.tblAssessments(BusinessId, BranchId, CaseYear, CaseSequence)
            WHERE CaseSequence > 0;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'business_cursor') = 1
        CLOSE business_cursor;
    IF CURSOR_STATUS('local', 'business_cursor') >= -1
        DEALLOCATE business_cursor;
    IF CURSOR_STATUS('local', 'branch_cursor') = 1
        CLOSE branch_cursor;
    IF CURSOR_STATUS('local', 'branch_cursor') >= -1
        DEALLOCATE branch_cursor;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

IF COLUMNPROPERTY(
        OBJECT_ID('dbo.tblAssessments'),
        'CaseNumber',
        'IsComputed') = 1
BEGIN
    EXEC sys.sp_rename
        'dbo.tblAssessments.CaseNumber',
        'LegacyComputedCaseNumber',
        'COLUMN';
    EXEC (
        'ALTER TABLE dbo.tblAssessments
         ADD CaseNumber varchar(64) NULL;'
    );
    EXEC (
        'UPDATE dbo.tblAssessments
         SET CaseNumber = LegacyComputedCaseNumber;'
    );
    EXEC (
        'ALTER TABLE dbo.tblAssessments
         DROP COLUMN LegacyComputedCaseNumber;'
    );
END;
GO

IF COL_LENGTH('dbo.tblAssessments', 'CaseNumber') IS NULL
    ALTER TABLE dbo.tblAssessments ADD CaseNumber varchar(64) NULL;
GO

ALTER TABLE dbo.tblAssessments
    ALTER COLUMN CaseNumber varchar(64) NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('dbo.tblAssessments')
      AND name = 'FK_tblAssessments_Branch'
)
    ALTER TABLE dbo.tblAssessments WITH CHECK
        ADD CONSTRAINT FK_tblAssessments_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.tblBranch(BranchId);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.tblAssessments')
      AND name = 'UX_tblAssessments_CaseNumber'
)
    CREATE UNIQUE INDEX UX_tblAssessments_CaseNumber
        ON dbo.tblAssessments(CaseNumber)
        WHERE CaseNumber IS NOT NULL;
GO

UPDATE dbo.tblTenantRole
SET RoleName = 'Site Manager',
    Description = REPLACE(
        REPLACE(Description, 'Branch', 'Site'),
        'branch',
        'site')
WHERE RoleCode = 'BRANCH_MANAGER';
GO
