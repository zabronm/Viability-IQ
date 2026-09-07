use ViabilityIQ;
-- ====================================================
-- ASSETS MANAGEMENT TABLES
-- ====================================================
-- These tables support comprehensive asset tracking for balance sheet generation
-- including depreciation, movements, and projections

-- ====================================================
-- 1. ASSET CATEGORIES TABLE
-- ====================================================
CREATE TABLE [dbo].[tblAssetCategories]
(
    [AssetCategoryId] BIGINT PRIMARY KEY IDENTITY(1,1),
    [CategoryName] NVARCHAR(100) NOT NULL,   
    [DisplayOrder] INT DEFAULT 0,
    [IsCurrentAsset] BIT DEFAULT 0,  -- 1 = Current Asset, 0 = Non-Current/Fixed Asset
    [BalanceSheetSection] NVARCHAR(100),
    [Active] BIT DEFAULT 1,
    [Remarks] NVARCHAR(500),

    [CreatedDate] DATETIME DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME NULL,
    [ModifiedBy] BIGINT NULL,   
    
    INDEX [IX_AssetCategories_Active] ([Active])
);

-- ====================================================
-- 2. ASSET TYPES TABLE
-- ====================================================
CREATE TABLE [dbo].[tblAssetTypes]
(
    [AssetTypeId] BIGINT PRIMARY KEY IDENTITY(1,1),
    [AssetCategoryId] BIGINT NOT NULL,
    [TypeName] NVARCHAR(100) NOT NULL,   
    [DefaultDepreciationRate] DECIMAL(5,2) DEFAULT 0.00,
    [DefaultUsefulLifeYears] INT,
    [IsDepreciable] BIT DEFAULT 1,
    [IsCurrent] BIT DEFAULT 0,
    [Active] BIT DEFAULT 1,
    [Remarks] NVARCHAR(500),

    [CreatedDate] DATETIME DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME NULL,
    [ModifiedBy] BIGINT NULL,    
    INDEX [IX_AssetTypes_AssetCategoryId] ([AssetCategoryId]),
    INDEX [IX_AssetTypes_Active] ([Active])
);

-- ====================================================
-- 3. MAIN ASSETS TABLE
-- ====================================================
CREATE TABLE [dbo].[tblAssessmentAssets]
(
    [AssessmentAssetId] BIGINT PRIMARY KEY IDENTITY(1,1),
    [AssessmentId] BIGINT NOT NULL,
    [AssetCategoryId] BIGINT,
    [AssetTypeId] BIGINT,
    
    -- Asset Identification
    [AssetName] NVARCHAR(200) NOT NULL,   
    [AssetReference] NVARCHAR(100),  -- Serial number, license plate, etc.
    
    -- Opening Balance (Assessment Start Date)
    [OpeningBalanceValue] DECIMAL(15,2) DEFAULT 0.00,
    [OpeningAccumulatedDepreciation] DECIMAL(15,2) DEFAULT 0.00,
    
    -- During Period Movements
    [AdditionsValue] DECIMAL(15,2) DEFAULT 0.00,
    [DisposalsValue] DECIMAL(15,2) DEFAULT 0.00,
    [DepreciationExpenseAmount] DECIMAL(15,2) DEFAULT 0.00,
    
    -- Closing Balance (Assessment End Date)
    [ClosingBalanceValue] DECIMAL(15,2) DEFAULT 0.00,
    [ClosingAccumulatedDepreciation] DECIMAL(15,2) DEFAULT 0.00,
    
    -- Depreciation Details
    [DepreciationMethod] NVARCHAR(50),  -- 'Straight-Line', 'Declining-Balance', 'Units-of-Production'
    [DepreciationRate] DECIMAL(5,2) DEFAULT 0.00,
    [UsefulLifeYears] INT,
    [EstimatedResidualValue] INT,
    
    -- Asset Classification
    [IsCurrentAsset] BIT DEFAULT 0,
    [IsDepreciable] BIT DEFAULT 1,
    [IsTangible] BIT DEFAULT 1,
    
    -- Acquisition Details
    [AcquisitionDate] DATETIME,
    [AcquisitionMethod] NVARCHAR(50),  -- 'Purchase', 'Donation', 'Manufacture', 'Trade-in'
    [FinancingStatus] NVARCHAR(50),    -- 'Owned', 'Leased', 'Financed', 'Mortgage'
    
    -- Valuation & Impairment
    [LastValuationDate] DATETIME,
    [FairValueAmount] DECIMAL(15,2),
    [ValuationBasis] NVARCHAR(50),  -- 'Historical Cost', 'Fair Value', 'Replacement Cost'
    [IsImpaired] BIT DEFAULT 0,
    [ImpairmentReason] NVARCHAR(500),
    
    -- Projections
    [ProjectedClosingValue] DECIMAL(15,2),
    [ProjectedDepreciation] DECIMAL(15,2),
    [ProjectionNotes] NVARCHAR(500),
    
    -- Audit & Metadata
    [Active] BIT DEFAULT 1,
    [Remarks] NVARCHAR(1000),
    [CreatedDate] DATETIME DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME NULL,
    [ModifiedBy] BIGINT NULL,  
    
    INDEX [IX_Assets_AssessmentId] ([AssessmentId]),
    INDEX [IX_Assets_AssetCategoryId] ([AssetCategoryId]),
    INDEX [IX_Assets_AssetTypeId] ([AssetTypeId]),
    INDEX [IX_Assets_Active] ([Active]),
    INDEX [IX_Assets_IsCurrentAsset] ([IsCurrentAsset]),
    INDEX [IX_Assets_IsDepreciable] ([IsDepreciable])
);

-- ====================================================
-- 4. ASSET DEPRECIATION TRACKING TABLE
-- ====================================================
CREATE TABLE [dbo].[tblAssetDepreciation]
(
    [AssetDepreciationId] BIGINT PRIMARY KEY IDENTITY(1,1),
    [AssessmentAssetId] BIGINT NOT NULL,
    [AssessmentId] BIGINT NOT NULL,
    [DepreciationDate] DATETIME NOT NULL,
    [DepreciationAmount] DECIMAL(15,2) NOT NULL,
    [AccumulatedDepreciationBefore] DECIMAL(15,2),
    [AccumulatedDepreciationAfter] DECIMAL(15,2),
    [Method] NVARCHAR(50),

    [Active] BIT DEFAULT 1,
    [Remarks] NVARCHAR(500),
    [CreatedDate] DATETIME DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME NULL,
    [ModifiedBy] BIGINT NULL,
    
    INDEX [IX_AssetDepreciation_AssetId] ([AssessmentAssetId]),
    INDEX [IX_AssetDepreciation_AssessmentId] ([AssessmentId]),
    INDEX [IX_AssetDepreciation_DepreciationDate] ([DepreciationDate])
);

-- ====================================================
-- 5. ASSET MOVEMENTS TABLE (Additions, Disposals, etc.)
-- ====================================================
CREATE TABLE [dbo].[tblAssetMovements]
(
    [AssetMovementId] BIGINT PRIMARY KEY IDENTITY(1,1),
    [AssessmentAssetId] BIGINT NOT NULL,
    [AssessmentId] BIGINT NOT NULL,
    [MovementDate] DATETIME NOT NULL,
    [MovementType] NVARCHAR(50) NOT NULL,  -- 'Addition', 'Disposal', 'Transfer', 'Revaluation'
    [MovementAmount] DECIMAL(15,2) NOT NULL,
    [Description] NVARCHAR(500),
    [Reference] NVARCHAR(100),  -- Invoice number, etc.

    [Active] BIT DEFAULT 1,
    [Remarks] NVARCHAR(1000),
    [CreatedDate] DATETIME DEFAULT GETUTCDATE(),
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedDate] DATETIME NULL,
    [ModifiedBy] BIGINT NULL,    
   
    INDEX [IX_AssetMovements_AssetId] ([AssessmentAssetId]),
    INDEX [IX_AssetMovements_AssessmentId] ([AssessmentId]),
    INDEX [IX_AssetMovements_MovementDate] ([MovementDate]),
    INDEX [IX_AssetMovements_MovementType] ([MovementType])
);

-- ====================================================
-- 6. SAMPLE DATA INSERTION - ASSET CATEGORIES
-- ====================================================
INSERT INTO [dbo].[tblAssetCategories] 
([CategoryName], Remarks, [DisplayOrder], [IsCurrentAsset], [BalanceSheetSection], [CreatedBy])
VALUES
    ('Fixed Assets', 'Long-term tangible assets', 1, 0, 'Non-Current Assets', 1),
    ('Current Assets', 'Short-term assets expected to convert to cash within a year', 2, 1, 'Current Assets', 1),
    ('Intangible Assets', 'Non-physical assets with long-term value', 3, 0, 'Non-Current Assets', 1),
    ('Investments', 'Long-term investments', 4, 0, 'Non-Current Assets', 1);

-- ====================================================
-- 7. SAMPLE DATA INSERTION - ASSET TYPES
-- ====================================================
INSERT INTO [dbo].[tblAssetTypes] 
([AssetCategoryId], [TypeName], Remarks, [DefaultDepreciationRate], [DefaultUsefulLifeYears], [IsDepreciable], [IsCurrent], [CreatedBy])
VALUES
    (1, 'Land', 'Buildings and land', 0, NULL, 0, 0, 1),
    (1, 'Buildings', 'Commercial and residential buildings', 2.5, 40, 1, 0, 1),
    (1, 'Machinery', 'Production and industrial machinery', 10, 10, 1, 0, 1),
    (1, 'Vehicles', 'Cars, trucks, and other vehicles', 20, 5, 1, 0, 1),
    (1, 'Furniture & Fixtures', 'Office furniture and fittings', 10, 10, 1, 0, 1),
    (2, 'Cash', 'Physical cash holdings', 0, NULL, 0, 1, 1),
    (2, 'Bank Accounts', 'Money in bank accounts', 0, NULL, 0, 1, 1),
    (2, 'Receivables', 'Outstanding customer payments', 0, NULL, 0, 1, 1),
    (2, 'Inventory', 'Stock of goods held for sale', 0, NULL, 0, 1, 1),
    (3, 'Goodwill', 'Premium paid over fair value in acquisition', 10, 10, 1, 0, 1),
    (3, 'Licenses & Permits', 'Business licenses and permits', 5, 20, 1, 0, 1),
    (4, 'Shares & Securities', 'Investments in other companies', 0, NULL, 0, 0, 1);
