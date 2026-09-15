-- ====================================================
-- SQL: Create AssetMonthlyMovement Table
-- Purpose: Store monthly asset movements for 12-month projections
-- Pattern: Flat 12-month table structure (like LoanRepayments)
-- ====================================================

-- ====================================================
-- TABLE: AssetMonthlyMovement
-- ====================================================
-- Stores the monthly movements for each asset
-- One record per asset per assessment with Month_1 through Month_12 columns
-- 
-- Data Structure:
-- - Opening balances inherited from tblAssets
-- - Each month has: MovementType, MovementValue, DepreciationAmount
-- - Calculated fields: GrossValue, AccumulatedDepreciation, NetBookValue
-- ====================================================

CREATE TABLE [dbo].[tblAssessmentAssetMovement]
(
    -- ====== PRIMARY KEY ======
    [AssessmentAssetMovementId] BIGINT PRIMARY KEY IDENTITY(1,1),

    -- ====== FOREIGN KEYS ======
    [AssessmentAssetId] BIGINT NOT NULL,
    [AssessmentId] BIGINT NOT NULL,
    [DepreciationRate] DECIMAL(18,2),                            -- Keep track of the depreciation rate that was used

    -- ====================================================
    -- MONTH 1
    -- ====================================================
    [Month_1_MovementType] NVARCHAR(50),                          -- Addition, Disposal, Revaluation, Transfer, or NULL
    [Month_1_MovementValue] DECIMAL(18,2) DEFAULT 0,             -- Amount of the movement
    [Month_1_Depreciation] DECIMAL(18,2) DEFAULT 0,              -- Depreciation expense for month
    [Month_1_GrossValue] DECIMAL(18,2),                          -- Calculated: Opening + Movement
    [Month_1_AccumulatedDepreciation] DECIMAL(18,2),             -- Calculated: Opening + Depreciation
    [Month_1_NetBookValue] DECIMAL(18,2),                        -- Calculated: Gross - Accumulated

    -- ====================================================
    -- MONTH 2
    -- ====================================================
    [Month_2_MovementType] NVARCHAR(50),
    [Month_2_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_2_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_2_GrossValue] DECIMAL(18,2),
    [Month_2_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_2_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 3
    -- ====================================================
    [Month_3_MovementType] NVARCHAR(50),
    [Month_3_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_3_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_3_GrossValue] DECIMAL(18,2),
    [Month_3_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_3_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 4
    -- ====================================================
    [Month_4_MovementType] NVARCHAR(50),
    [Month_4_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_4_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_4_GrossValue] DECIMAL(18,2),
    [Month_4_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_4_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 5
    -- ====================================================
    [Month_5_MovementType] NVARCHAR(50),
    [Month_5_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_5_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_5_GrossValue] DECIMAL(18,2),
    [Month_5_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_5_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 6
    -- ====================================================
    [Month_6_MovementType] NVARCHAR(50),
    [Month_6_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_6_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_6_GrossValue] DECIMAL(18,2),
    [Month_6_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_6_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 7
    -- ====================================================
    [Month_7_MovementType] NVARCHAR(50),
    [Month_7_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_7_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_7_GrossValue] DECIMAL(18,2),
    [Month_7_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_7_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 8
    -- ====================================================
    [Month_8_MovementType] NVARCHAR(50),
    [Month_8_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_8_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_8_GrossValue] DECIMAL(18,2),
    [Month_8_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_8_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 9
    -- ====================================================
    [Month_9_MovementType] NVARCHAR(50),
    [Month_9_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_9_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_9_GrossValue] DECIMAL(18,2),
    [Month_9_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_9_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 10
    -- ====================================================
    [Month_10_MovementType] NVARCHAR(50),
    [Month_10_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_10_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_10_GrossValue] DECIMAL(18,2),
    [Month_10_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_10_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 11
    -- ====================================================
    [Month_11_MovementType] NVARCHAR(50),
    [Month_11_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_11_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_11_GrossValue] DECIMAL(18,2),
    [Month_11_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_11_NetBookValue] DECIMAL(18,2),

    -- ====================================================
    -- MONTH 12
    -- ====================================================
    [Month_12_MovementType] NVARCHAR(50),
    [Month_12_MovementValue] DECIMAL(18,2) DEFAULT 0,
    [Month_12_Depreciation] DECIMAL(18,2) DEFAULT 0,
    [Month_12_GrossValue] DECIMAL(18,2),
    [Month_12_AccumulatedDepreciation] DECIMAL(18,2),
    [Month_12_NetBookValue] DECIMAL(18,2),

    -- ====== AUDIT TRAIL ======
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME2,
    [ModifiedBy] BIGINT,
    [Active] BIT NOT NULL DEFAULT 1,
    [Remarks] NVARCHAR(MAX),

    ---- ====== CONSTRAINTS ======
    --CONSTRAINT FK_AssetMonthlyMovement_Asset 
    --    FOREIGN KEY ([AssessmentAssetId]) REFERENCES [dbo].[AssessmentAsset]([AssessmentAssetId]),
    
    --CONSTRAINT FK_AssetMonthlyMovement_Assessment 
    --    FOREIGN KEY ([AssessmentId]) REFERENCES [dbo].[Assessment]([AssessmentId]),
    
    --CONSTRAINT UQ_AssetMonthlyMovement_Unique 
    --    UNIQUE ([AssessmentAssetId], [AssessmentId])
);

-- ====== INDEXES ======
CREATE NONCLUSTERED INDEX [IX_AssetMonthlyMovement_Assessment]
    ON [dbo].[AssetMonthlyMovement]([AssessmentId])
    INCLUDE ([AssessmentAssetId], [Month_1_NetBookValue], [Month_12_NetBookValue]);

CREATE NONCLUSTERED INDEX [IX_AssetMonthlyMovement_Asset]
    ON [dbo].[AssetMonthlyMovement]([AssessmentAssetId])
    INCLUDE ([AssessmentId], [Month_1_GrossValue], [Month_12_NetBookValue]);

-- ====================================================
-- VIEW: vw_AssetMonthlyProjection
-- Purpose: Flatten monthly data into a queryable format
-- ====================================================

CREATE VIEW [dbo].[vw_Asset_monthly_projection]
AS
SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName] AS AssetType,
    ac.[CategoryName] AS AssetCategory,
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    
    -- Month 1
    1 AS [MonthNumber],
    amm.[Month_1_MovementType],
    amm.[Month_1_MovementValue],
    amm.[Month_1_Depreciation],
    amm.[Month_1_GrossValue],
    amm.[Month_1_AccumulatedDepreciation],
    amm.[Month_1_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[tblAssessmentAssets] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[tblAssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[tblAssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    2, amm.[Month_2_MovementType], amm.[Month_2_MovementValue], amm.[Month_2_Depreciation],
    amm.[Month_2_GrossValue], amm.[Month_2_AccumulatedDepreciation], amm.[Month_2_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    3, amm.[Month_3_MovementType], amm.[Month_3_MovementValue], amm.[Month_3_Depreciation],
    amm.[Month_3_GrossValue], amm.[Month_3_AccumulatedDepreciation], amm.[Month_3_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    4, amm.[Month_4_MovementType], amm.[Month_4_MovementValue], amm.[Month_4_Depreciation],
    amm.[Month_4_GrossValue], amm.[Month_4_AccumulatedDepreciation], amm.[Month_4_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    5, amm.[Month_5_MovementType], amm.[Month_5_MovementValue], amm.[Month_5_Depreciation],
    amm.[Month_5_GrossValue], amm.[Month_5_AccumulatedDepreciation], amm.[Month_5_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    6, amm.[Month_6_MovementType], amm.[Month_6_MovementValue], amm.[Month_6_Depreciation],
    amm.[Month_6_GrossValue], amm.[Month_6_AccumulatedDepreciation], amm.[Month_6_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    7, amm.[Month_7_MovementType], amm.[Month_7_MovementValue], amm.[Month_7_Depreciation],
    amm.[Month_7_GrossValue], amm.[Month_7_AccumulatedDepreciation], amm.[Month_7_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    8, amm.[Month_8_MovementType], amm.[Month_8_MovementValue], amm.[Month_8_Depreciation],
    amm.[Month_8_GrossValue], amm.[Month_8_AccumulatedDepreciation], amm.[Month_8_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    9, amm.[Month_9_MovementType], amm.[Month_9_MovementValue], amm.[Month_9_Depreciation],
    amm.[Month_9_GrossValue], amm.[Month_9_AccumulatedDepreciation], amm.[Month_9_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    10, amm.[Month_10_MovementType], amm.[Month_10_MovementValue], amm.[Month_10_Depreciation],
    amm.[Month_10_GrossValue], amm.[Month_10_AccumulatedDepreciation], amm.[Month_10_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    11, amm.[Month_11_MovementType], amm.[Month_11_MovementValue], amm.[Month_11_Depreciation],
    amm.[Month_11_GrossValue], amm.[Month_11_AccumulatedDepreciation], amm.[Month_11_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId]

UNION ALL

SELECT 
    amm.[AssessmentAssetId],
    amm.[AssessmentId],
    aa.[AssetName],
    at.[TypeName],
    ac.[CategoryName],
    aa.[OpeningBalanceValue],
    aa.[OpeningAccumulatedDepreciation],
    aa.[DepreciationRate],
    aa.[DepreciationMethod],
    12, amm.[Month_12_MovementType], amm.[Month_12_MovementValue], amm.[Month_12_Depreciation],
    amm.[Month_12_GrossValue], amm.[Month_12_AccumulatedDepreciation], amm.[Month_12_NetBookValue]
FROM [dbo].[AssetMonthlyMovement] amm
INNER JOIN [dbo].[AssessmentAsset] aa ON amm.[AssessmentAssetId] = aa.[AssessmentAssetId]
LEFT JOIN [dbo].[AssetType] at ON aa.[AssetTypeId] = at.[AssetTypeId]
LEFT JOIN [dbo].[AssetCategory] ac ON at.[AssetCategoryId] = ac.[AssetCategoryId];

GO

-- ====================================================
-- EXAMPLE QUERIES
-- ====================================================

-- Get all months for a specific asset
-- SELECT * FROM vw_AssetMonthlyProjection WHERE AssessmentAssetId = 1 ORDER BY MonthNumber;

-- Get final month balances for all assets in an assessment
-- SELECT AssessmentAssetId, AssetName, Month_12_GrossValue, Month_12_AccumulatedDepreciation, Month_12_NetBookValue 
-- FROM dbo.AssetMonthlyMovement amm
-- WHERE AssessmentId = 123;

-- Get total asset values by month for an assessment
-- SELECT 
--     MonthNumber,
--     SUM(CASE WHEN MonthNumber = 1 THEN Month_1_GrossValue ELSE 0 END) AS TotalGrossValue,
--     SUM(CASE WHEN MonthNumber = 1 THEN Month_1_NetBookValue ELSE 0 END) AS TotalNetValue
-- FROM vw_AssetMonthlyProjection
-- WHERE AssessmentId = 123
-- GROUP BY MonthNumber;
