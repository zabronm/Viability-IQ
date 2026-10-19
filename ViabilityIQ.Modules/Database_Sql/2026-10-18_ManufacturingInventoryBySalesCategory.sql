SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tblAssessmentSalesCategory', 'U') IS NULL
    THROW 50062, 'dbo.tblAssessmentSalesCategory does not exist.', 1;
GO

IF OBJECT_ID('dbo.tblAssessmentManufacturingMaterials', 'U') IS NULL
    THROW 50063, 'dbo.tblAssessmentManufacturingMaterials does not exist.', 1;
GO

IF OBJECT_ID('dbo.tblAssessmentManufacturingBalances', 'U') IS NULL
    THROW 50064, 'dbo.tblAssessmentManufacturingBalances does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessmentManufacturingMaterials', 'AssessmentSalesCategoryId') IS NULL
    ALTER TABLE dbo.tblAssessmentManufacturingMaterials
        ADD AssessmentSalesCategoryId bigint NULL;
GO

IF COL_LENGTH('dbo.tblAssessmentManufacturingBalances', 'AssessmentSalesCategoryId') IS NULL
    ALTER TABLE dbo.tblAssessmentManufacturingBalances
        ADD AssessmentSalesCategoryId bigint NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_AMM_AssessmentSalesCategory'
      AND parent_object_id = OBJECT_ID('dbo.tblAssessmentManufacturingMaterials')
)
    ALTER TABLE dbo.tblAssessmentManufacturingMaterials
        ADD CONSTRAINT FK_AMM_AssessmentSalesCategory
            FOREIGN KEY (AssessmentSalesCategoryId)
            REFERENCES dbo.tblAssessmentSalesCategory (AssessmentSalesCategoryId);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_AMB_AssessmentSalesCategory'
      AND parent_object_id = OBJECT_ID('dbo.tblAssessmentManufacturingBalances')
)
    ALTER TABLE dbo.tblAssessmentManufacturingBalances
        ADD CONSTRAINT FK_AMB_AssessmentSalesCategory
            FOREIGN KEY (AssessmentSalesCategoryId)
            REFERENCES dbo.tblAssessmentSalesCategory (AssessmentSalesCategoryId);
GO

IF EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_tblAssessmentManufacturingBalances_AssessmentId'
      AND parent_object_id = OBJECT_ID('dbo.tblAssessmentManufacturingBalances')
)
    ALTER TABLE dbo.tblAssessmentManufacturingBalances
        DROP CONSTRAINT UQ_tblAssessmentManufacturingBalances_AssessmentId;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_AMB_Assessment_Category'
      AND object_id = OBJECT_ID('dbo.tblAssessmentManufacturingBalances')
)
    CREATE UNIQUE INDEX UX_AMB_Assessment_Category
        ON dbo.tblAssessmentManufacturingBalances (AssessmentId, AssessmentSalesCategoryId)
        WHERE AssessmentSalesCategoryId IS NOT NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_AMB_Assessment_Unassigned'
      AND object_id = OBJECT_ID('dbo.tblAssessmentManufacturingBalances')
)
    CREATE UNIQUE INDEX UX_AMB_Assessment_Unassigned
        ON dbo.tblAssessmentManufacturingBalances (AssessmentId)
        WHERE AssessmentSalesCategoryId IS NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_AMM_Assessment_Category_Active'
      AND object_id = OBJECT_ID('dbo.tblAssessmentManufacturingMaterials')
)
    CREATE INDEX IX_AMM_Assessment_Category_Active
        ON dbo.tblAssessmentManufacturingMaterials
            (AssessmentId, AssessmentSalesCategoryId, Active);
GO
