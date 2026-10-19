SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tblAssessmentStock', 'U') IS NULL
    THROW 50041, 'dbo.tblAssessmentStock does not exist.', 1;
GO

IF OBJECT_ID('dbo.tblAssessmentSalesCategory', 'U') IS NULL
    THROW 50042, 'dbo.tblAssessmentSalesCategory does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'OpeningBalance') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD OpeningBalance decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_OpeningBalance DEFAULT (0)
            WITH VALUES;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE stock
    SET Description = COALESCE(
        NULLIF(LTRIM(RTRIM(category.AssessmentSalesCategoryName)), ''),
        CONCAT('Stock item ', stock.AssessmentStockId))
    FROM dbo.tblAssessmentStock stock
    LEFT JOIN dbo.tblAssessmentSalesCategory category
        ON category.AssessmentSalesCategoryId = stock.AssessmentSalesCategoryId
    WHERE NULLIF(LTRIM(RTRIM(stock.Description)), '') IS NULL;

    ;WITH ExistingStockItems AS
    (
        SELECT
            stock.AssessmentStockId,
            category.OpeningStock,
            ROW_NUMBER() OVER
            (
                PARTITION BY category.AssessmentSalesCategoryId
                ORDER BY stock.AssessmentStockId
            ) AS ItemNumber
        FROM dbo.tblAssessmentSalesCategory category
        INNER JOIN dbo.tblAssessmentStock stock
            ON stock.AssessmentSalesCategoryId = category.AssessmentSalesCategoryId
           AND stock.AssessmentId = category.AssessmentId
        WHERE category.OpeningStock <> 0
    )
    UPDATE stock
    SET OpeningBalance = existing.OpeningStock
    FROM dbo.tblAssessmentStock stock
    INNER JOIN ExistingStockItems existing
        ON existing.AssessmentStockId = stock.AssessmentStockId
       AND existing.ItemNumber = 1;

    INSERT INTO dbo.tblAssessmentStock
    (
        AssessmentId,
        AssessmentSalesCategoryId,
        Description,
        blIncludeVAT,
        SameMonthlyAmount,
        Month_1,
        Month_2,
        Month_3,
        Month_4,
        Month_5,
        Month_6,
        Month_7,
        Month_8,
        Month_9,
        Month_10,
        Month_11,
        Month_12,
        TotalNoVAT,
        TotalWithVAT,
        Active,
        Remarks,
        CreatedDate,
        CreatedBy,
        ModifiedDate,
        ModifiedBy,
        OpeningBalance
    )
    SELECT
        category.AssessmentId,
        category.AssessmentSalesCategoryId,
        CONCAT(category.AssessmentSalesCategoryName, ' - opening inventory'),
        0,
        0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0,
        0,
        category.Active,
        category.Remarks,
        SYSUTCDATETIME(),
        category.CreatedBy,
        SYSUTCDATETIME(),
        category.ModifiedBy,
        category.OpeningStock
    FROM dbo.tblAssessmentSalesCategory category
    WHERE category.OpeningStock <> 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.tblAssessmentStock stock
          WHERE stock.AssessmentId = category.AssessmentId
            AND stock.AssessmentSalesCategoryId = category.AssessmentSalesCategoryId
      );

    UPDATE dbo.tblAssessmentSalesCategory
    SET OpeningStock = 0
    WHERE OpeningStock <> 0;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
