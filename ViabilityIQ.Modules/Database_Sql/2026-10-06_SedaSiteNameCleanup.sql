SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.tblBranch', 'BranchCode') IS NULL
        THROW 50001, 'Run 2026-10-06_AssessmentCaseNumbers.sql before this cleanup.', 1;

    DECLARE
        @BranchId bigint,
        @TenantId bigint,
        @CurrentName nvarchar(500),
        @SiteName nvarchar(500),
        @Normalized varchar(200),
        @Character nchar(1),
        @Position int,
        @BaseCode varchar(3),
        @Candidate varchar(12),
        @Suffix int;

    DECLARE @UpdatedSites TABLE
    (
        BranchId bigint NOT NULL,
        TenantId bigint NULL,
        PreviousBranchName nvarchar(500) NULL,
        BranchName nvarchar(500) NULL,
        PreviousBranchCode varchar(12) NULL,
        BranchCode varchar(12) NULL
    );

    DECLARE site_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT BranchId, TenantId, LTRIM(RTRIM(BranchName))
        FROM dbo.tblBranch WITH (UPDLOCK, HOLDLOCK)
        WHERE UPPER(LTRIM(RTRIM(BranchName))) LIKE 'SEDA %'
          AND UPPER(LTRIM(RTRIM(BranchName))) LIKE '% BRANCH'
        ORDER BY TenantId, BranchId;

    OPEN site_cursor;
    FETCH NEXT FROM site_cursor
        INTO @BranchId, @TenantId, @CurrentName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SiteName = LTRIM(RTRIM(@CurrentName));

        IF UPPER(LEFT(@SiteName, 5)) = 'SEDA '
            SET @SiteName = LTRIM(SUBSTRING(@SiteName, 6, LEN(@SiteName)));

        IF UPPER(RIGHT(@SiteName, 7)) = ' BRANCH'
            SET @SiteName = RTRIM(LEFT(@SiteName, LEN(@SiteName) - 7));

        IF NULLIF(@SiteName, '') IS NULL
            THROW 50002, 'A SEDA Site name became empty during cleanup.', 1;

        SET @Normalized = '';
        SET @Position = 1;
        WHILE @Position <= LEN(@SiteName)
        BEGIN
            SET @Character = UPPER(SUBSTRING(@SiteName, @Position, 1));
            IF @Character LIKE '[A-Z0-9]'
                SET @Normalized += @Character;
            SET @Position += 1;
        END;

        IF LEN(@Normalized) < 3
            THROW 50003, 'A cleaned Site name has fewer than three letters or numbers.', 1;

        SET @BaseCode = LEFT(@Normalized, 3);
        SET @Candidate = @BaseCode;
        SET @Suffix = 0;

        WHILE EXISTS
        (
            SELECT 1
            FROM dbo.tblBranch
            WHERE TenantId = @TenantId
              AND BranchCode = @Candidate
              AND BranchId <> @BranchId
        )
        BEGIN
            SET @Candidate =
                LEFT(@BaseCode, 12 - LEN(CONVERT(varchar(10), @Suffix)))
                + CONVERT(varchar(10), @Suffix);
            SET @Suffix += 1;
        END;

        UPDATE dbo.tblBranch
        SET BranchName = @SiteName,
            BranchCode = @Candidate,
            ModifiedDate = SYSUTCDATETIME()
        OUTPUT
            inserted.BranchId,
            inserted.TenantId,
            deleted.BranchName,
            inserted.BranchName,
            deleted.BranchCode,
            inserted.BranchCode
        INTO @UpdatedSites
        WHERE BranchId = @BranchId;

        FETCH NEXT FROM site_cursor
            INTO @BranchId, @TenantId, @CurrentName;
    END;

    CLOSE site_cursor;
    DEALLOCATE site_cursor;

    SELECT
        BranchId,
        TenantId,
        PreviousBranchName,
        BranchName,
        PreviousBranchCode,
        BranchCode
    FROM @UpdatedSites
    ORDER BY TenantId, BranchName;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'site_cursor') = 1
        CLOSE site_cursor;
    IF CURSOR_STATUS('local', 'site_cursor') >= -1
        DEALLOCATE site_cursor;
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
