SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.tblProvince', 'TenantId') IS NULL
    ALTER TABLE dbo.tblProvince ADD TenantId bigint NULL;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @RegionMap TABLE
    (
        TenantId bigint NOT NULL,
        OldProvinceId bigint NOT NULL,
        NewProvinceId bigint NOT NULL,
        PRIMARY KEY (TenantId, OldProvinceId)
    );

    DECLARE
        @TenantId bigint,
        @OldProvinceId bigint,
        @NewProvinceId bigint,
        @ProvinceName nvarchar(500);

    DECLARE region_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT source.TenantId, source.ProvinceId
        FROM
        (
            SELECT TenantId, ProvinceId FROM dbo.tblTenant
            WHERE TenantId IS NOT NULL AND ProvinceId IS NOT NULL
            UNION
            SELECT TenantId, ProvinceId FROM dbo.tblCompany
            WHERE TenantId IS NOT NULL AND ProvinceId IS NOT NULL
            UNION
            SELECT TenantId, ProvinceId FROM dbo.tblBranch
            WHERE TenantId IS NOT NULL AND ProvinceId IS NOT NULL
            UNION
            SELECT TenantId, ProvinceId FROM dbo.tblBusiness
            WHERE TenantId IS NOT NULL AND ProvinceId IS NOT NULL
            UNION
            SELECT TenantId, ProvinceId FROM dbo.tblClient
            WHERE TenantId IS NOT NULL AND ProvinceId IS NOT NULL
        ) source
        INNER JOIN dbo.tblProvince province
            ON province.ProvinceId = source.ProvinceId
        ORDER BY source.TenantId, source.ProvinceId;

    OPEN region_cursor;
    FETCH NEXT FROM region_cursor
        INTO @TenantId, @OldProvinceId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @ProvinceName = ProvinceName
        FROM dbo.tblProvince
        WHERE ProvinceId = @OldProvinceId;

        SELECT @NewProvinceId = ProvinceId
        FROM dbo.tblProvince
        WHERE TenantId = @TenantId
          AND ProvinceName = @ProvinceName;

        IF @NewProvinceId IS NULL
        BEGIN
            INSERT INTO dbo.tblProvince
                (TenantId, ShortName, ProvinceName, Address_Street,
                 Address_Location, Address_Suburb, Address_City,
                 Manager_UserId, Telephone, Mobile, Email,
                 Address_Postal, Address_PostalLocation,
                 Address_PostalCity, Address_PostalCode,
                 Active, Remarks, CreatedDate, CreatedBy,
                 ModifiedDate, ModifiedBy)
            SELECT
                @TenantId, ShortName, ProvinceName, Address_Street,
                Address_Location, Address_Suburb, Address_City,
                Manager_UserId, Telephone, Mobile, Email,
                Address_Postal, Address_PostalLocation,
                Address_PostalCity, Address_PostalCode,
                Active, Remarks, CreatedDate, CreatedBy,
                ModifiedDate, ModifiedBy
            FROM dbo.tblProvince
            WHERE ProvinceId = @OldProvinceId;

            SET @NewProvinceId = SCOPE_IDENTITY();
        END;

        INSERT INTO @RegionMap (TenantId, OldProvinceId, NewProvinceId)
        VALUES (@TenantId, @OldProvinceId, @NewProvinceId);

        SET @NewProvinceId = NULL;
        FETCH NEXT FROM region_cursor
            INTO @TenantId, @OldProvinceId;
    END;

    CLOSE region_cursor;
    DEALLOCATE region_cursor;

    UPDATE tenantRecord
    SET ProvinceId = regionMap.NewProvinceId
    FROM dbo.tblTenant tenantRecord
    INNER JOIN @RegionMap regionMap
        ON regionMap.TenantId = tenantRecord.TenantId
       AND regionMap.OldProvinceId = tenantRecord.ProvinceId;

    UPDATE company
    SET ProvinceId = regionMap.NewProvinceId
    FROM dbo.tblCompany company
    INNER JOIN @RegionMap regionMap
        ON regionMap.TenantId = company.TenantId
       AND regionMap.OldProvinceId = company.ProvinceId;

    UPDATE branch
    SET ProvinceId = regionMap.NewProvinceId
    FROM dbo.tblBranch branch
    INNER JOIN @RegionMap regionMap
        ON regionMap.TenantId = branch.TenantId
       AND regionMap.OldProvinceId = branch.ProvinceId;

    UPDATE business
    SET ProvinceId = regionMap.NewProvinceId
    FROM dbo.tblBusiness business
    INNER JOIN @RegionMap regionMap
        ON regionMap.TenantId = business.TenantId
       AND regionMap.OldProvinceId = business.ProvinceId;

    UPDATE client
    SET ProvinceId = regionMap.NewProvinceId
    FROM dbo.tblClient client
    INNER JOIN @RegionMap regionMap
        ON regionMap.TenantId = client.TenantId
       AND regionMap.OldProvinceId = client.ProvinceId;

    UPDATE applicationUser
    SET ProvinceId = branch.ProvinceId,
        BranchId = membership.DefaultBranchId
    FROM dbo.tblApplicationUsers applicationUser
    INNER JOIN dbo.tblTenantMembership membership
        ON membership.UserId = applicationUser.UserId
       AND membership.Active = 1
       AND membership.MembershipStatus = 'Active'
    INNER JOIN dbo.tblBranch branch
        ON branch.BranchId = membership.DefaultBranchId
       AND branch.TenantId = membership.TenantId
    WHERE membership.IsOwner = 1;

    INSERT INTO dbo.tblCompany
        (TenantId, CompanyName, ContactPerson, ProvinceId,
         Active, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
    SELECT
        tenantRecord.TenantId,
        tenantRecord.TenantName,
        COALESCE(
            NULLIF(LTRIM(RTRIM(CONCAT(ownerUser.FirstName, ' ', ownerUser.LastName))), ''),
            ownerUser.Email,
            tenantRecord.TenantName),
        tenantRecord.ProvinceId,
        1,
        SYSUTCDATETIME(),
        tenantRecord.OwnerUserId,
        SYSUTCDATETIME(),
        tenantRecord.OwnerUserId
    FROM dbo.tblTenant tenantRecord
    LEFT JOIN dbo.tblApplicationUsers ownerUser
        ON ownerUser.UserId = tenantRecord.OwnerUserId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tblCompany company
        WHERE company.TenantId = tenantRecord.TenantId
    );

    IF EXISTS
    (
        SELECT TenantId
        FROM dbo.tblCompany
        WHERE TenantId IS NOT NULL
        GROUP BY TenantId
        HAVING COUNT_BIG(1) > 1
    )
        THROW 50011, 'A tenant has more than one Company record. Resolve duplicates before applying the unique Company constraint.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblCompany')
          AND name = 'UX_tblCompany_Tenant'
    )
        CREATE UNIQUE INDEX UX_tblCompany_Tenant
            ON dbo.tblCompany(TenantId)
            WHERE TenantId IS NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblProvince')
          AND name = 'UX_tblProvince_Tenant_Name'
    )
        CREATE UNIQUE INDEX UX_tblProvince_Tenant_Name
            ON dbo.tblProvince(TenantId, ProvinceName)
            WHERE TenantId IS NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblProvince')
          AND name = 'UX_tblProvince_Tenant_Id'
    )
        CREATE UNIQUE INDEX UX_tblProvince_Tenant_Id
            ON dbo.tblProvince(TenantId, ProvinceId);

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.tblBranch')
          AND name = 'UX_tblBranch_Tenant_Id'
    )
        CREATE UNIQUE INDEX UX_tblBranch_Tenant_Id
            ON dbo.tblBranch(TenantId, BranchId);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_tblCompany_Tenant'
          AND parent_object_id = OBJECT_ID('dbo.tblCompany')
    )
        ALTER TABLE dbo.tblCompany WITH CHECK
            ADD CONSTRAINT FK_tblCompany_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_tblProvince_Tenant'
          AND parent_object_id = OBJECT_ID('dbo.tblProvince')
    )
        ALTER TABLE dbo.tblProvince WITH CHECK
            ADD CONSTRAINT FK_tblProvince_Tenant
            FOREIGN KEY (TenantId) REFERENCES dbo.tblTenant(TenantId);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_tblBranch_TenantRegion'
          AND parent_object_id = OBJECT_ID('dbo.tblBranch')
    )
        ALTER TABLE dbo.tblBranch WITH CHECK
            ADD CONSTRAINT FK_tblBranch_TenantRegion
            FOREIGN KEY (TenantId, ProvinceId)
            REFERENCES dbo.tblProvince(TenantId, ProvinceId);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_tblTenantMembership_TenantSite'
          AND parent_object_id = OBJECT_ID('dbo.tblTenantMembership')
    )
        ALTER TABLE dbo.tblTenantMembership WITH CHECK
            ADD CONSTRAINT FK_tblTenantMembership_TenantSite
            FOREIGN KEY (TenantId, DefaultBranchId)
            REFERENCES dbo.tblBranch(TenantId, BranchId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'region_cursor') = 1
        CLOSE region_cursor;
    IF CURSOR_STATUS('local', 'region_cursor') >= -1
        DEALLOCATE region_cursor;
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
