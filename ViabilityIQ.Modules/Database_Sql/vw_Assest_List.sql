ALTER VIEW [dbo].[vw_assessment_assets_list]
WITH SCHEMABINDING
AS
SELECT 
    -- ====================================================
    -- ASSET CORE FIELDS
    -- ====================================================
    a.[AssessmentAssetId] AS [AssessmentAssetId],
    a.[AssessmentId],
    a.[AssetName],
    a.[Remarks],
    a.[AssetReference],
    
    -- ====================================================
    -- CATEGORY FIELDS
    -- ====================================================
    a.[AssetCategoryId],
    ac.[CategoryName] AS [AssetCategoryName],
    ac.[BalanceSheetSection],
    
    -- ====================================================
    -- TYPE FIELDS
    -- ====================================================
    a.[AssetTypeId],
    at.[TypeName] AS [AssetTypeName],
    at.[DefaultDepreciationRate],
    
    -- ====================================================
    -- OPENING BALANCE
    -- ====================================================
    a.[OpeningBalanceValue],
    a.[OpeningAccumulatedDepreciation],
    (a.[OpeningBalanceValue] - a.[OpeningAccumulatedDepreciation]) AS [OpeningNetValue],
    
    -- ====================================================
    -- CLOSING BALANCE
    -- ====================================================
    a.[ClosingBalanceValue],
    a.[ClosingAccumulatedDepreciation],
    (a.[ClosingBalanceValue] - a.[ClosingAccumulatedDepreciation]) AS [ClosingNetValue],
    
    -- ====================================================
    -- DEPRECIATION
    -- ====================================================
    a.[DepreciationMethod],
    a.[DepreciationRate],
    a.[UsefulLifeYears],
    
    -- ====================================================
    -- MOVEMENTS
    -- ====================================================
    a.[AdditionsValue],
    a.[DisposalsValue],
    a.[DepreciationExpenseAmount],
    
    -- ====================================================
    -- CLASSIFICATION
    -- ====================================================
    a.[IsCurrentAsset],
    a.[IsDepreciable],
    a.[IsTangible],
    
    -- ====================================================
    -- VALUATION & IMPAIRMENT
    -- ====================================================
    a.[IsImpaired],
    a.[ImpairmentReason],
    a.[FairValueAmount],
    a.[ValuationBasis],
    
    -- ====================================================
    -- AUDIT TRAIL
    -- ====================================================
    a.[CreatedDate],
    a.[CreatedBy],
    a.[ModifiedDate],
    a.[ModifiedBy],
    a.[Active]

FROM [dbo].[tblAssessmentAssets] a
LEFT JOIN [dbo].[tblAssetCategory] ac ON a.[AssetCategoryId] = ac.[AssetCategoryId]
LEFT JOIN [dbo].[tblAssetType] at ON a.[AssetTypeId] = at.[AssetTypeId]
WHERE a.[Active] = 1;
GO

