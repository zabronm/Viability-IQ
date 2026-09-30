SET XACT_ABORT ON;

DECLARE @AssessmentId BIGINT = 4;

BEGIN TRANSACTION;

-- Restore category-backed rows whose relationship survived but whose Active
-- flag was cleared by the former partial-entity edit form.
UPDATE sales
SET
    sales.Active = 1,
    sales.IncomeTypeId = category.IncomeTypeId,
    sales.ModifiedDate = SYSUTCDATETIME()
FROM dbo.tblAssessmentSales AS sales
INNER JOIN dbo.tblAssessmentSalesCategory AS category
    ON category.AssessmentSalesCategoryId = sales.ProductCategoryId
   AND category.AssessmentId = sales.AssessmentId
   AND category.Active = 1
WHERE sales.AssessmentId = @AssessmentId
  AND sales.Active = 0
  AND
  (
      sales.Month_1 <> 0 OR sales.Month_2 <> 0 OR sales.Month_3 <> 0
      OR sales.Month_4 <> 0 OR sales.Month_5 <> 0 OR sales.Month_6 <> 0
      OR sales.Month_7 <> 0 OR sales.Month_8 <> 0 OR sales.Month_9 <> 0
      OR sales.Month_10 <> 0 OR sales.Month_11 <> 0 OR sales.Month_12 <> 0
  );

-- Repair rows whose ProductCategoryId was overwritten with zero. A row is
-- updated only when its description matches exactly one active category in
-- the same assessment, preventing an ambiguous automatic link.
;WITH CategoryMatches AS
(
    SELECT
        sales.AssessmentSalesId,
        MAX(category.AssessmentSalesCategoryId) AS AssessmentSalesCategoryId,
        MAX(category.IncomeTypeId) AS IncomeTypeId,
        COUNT_BIG(*) AS MatchCount
    FROM dbo.tblAssessmentSales AS sales
    INNER JOIN dbo.tblAssessmentSalesCategory AS category
        ON category.AssessmentId = sales.AssessmentId
       AND category.Active = 1
       AND LOWER(LTRIM(RTRIM(category.AssessmentSalesCategoryName)))
           = LOWER(LTRIM(RTRIM(sales.Description)))
    WHERE sales.AssessmentId = @AssessmentId
      AND
      (
          sales.ProductCategoryId = 0
          OR sales.ProductCategoryId IS NULL
      )
      AND
      (
          sales.Month_1 <> 0 OR sales.Month_2 <> 0 OR sales.Month_3 <> 0
          OR sales.Month_4 <> 0 OR sales.Month_5 <> 0 OR sales.Month_6 <> 0
          OR sales.Month_7 <> 0 OR sales.Month_8 <> 0 OR sales.Month_9 <> 0
          OR sales.Month_10 <> 0 OR sales.Month_11 <> 0 OR sales.Month_12 <> 0
      )
    GROUP BY sales.AssessmentSalesId
)
UPDATE sales
SET
    sales.ProductCategoryId = matches.AssessmentSalesCategoryId,
    sales.IncomeTypeId = matches.IncomeTypeId,
    sales.Active = 1,
    sales.ModifiedDate = SYSUTCDATETIME()
FROM dbo.tblAssessmentSales AS sales
INNER JOIN CategoryMatches AS matches
    ON matches.AssessmentSalesId = sales.AssessmentSalesId
WHERE matches.MatchCount = 1;

-- Recalculate persisted annual totals without assuming a hard-coded VAT rate.
UPDATE sales
SET
    sales.TotalNoVAT =
        sales.Month_1 + sales.Month_2 + sales.Month_3 + sales.Month_4
        + sales.Month_5 + sales.Month_6 + sales.Month_7 + sales.Month_8
        + sales.Month_9 + sales.Month_10 + sales.Month_11 + sales.Month_12,
    sales.TotalWithVAT =
        CASE
            WHEN sales.IncludeVAT <> 0
                THEN
                    (
                        sales.Month_1 + sales.Month_2 + sales.Month_3 + sales.Month_4
                        + sales.Month_5 + sales.Month_6 + sales.Month_7 + sales.Month_8
                        + sales.Month_9 + sales.Month_10 + sales.Month_11 + sales.Month_12
                    ) * (1 + COALESCE(NULLIF(sales.VATRate, 0), 15) / 100)
            ELSE
                sales.Month_1 + sales.Month_2 + sales.Month_3 + sales.Month_4
                + sales.Month_5 + sales.Month_6 + sales.Month_7 + sales.Month_8
                + sales.Month_9 + sales.Month_10 + sales.Month_11 + sales.Month_12
        END,
    sales.ModifiedDate = SYSUTCDATETIME()
FROM dbo.tblAssessmentSales AS sales
WHERE sales.AssessmentId = @AssessmentId
  AND sales.Active = 1;

SELECT
    sales.AssessmentSalesId,
    sales.AssessmentId,
    sales.ProductCategoryId,
    sales.IncomeTypeId,
    incomeType.IncomeTypeName,
    sales.Description,
    sales.Active,
    sales.TotalNoVAT,
    sales.TotalWithVAT
FROM dbo.tblAssessmentSales AS sales
LEFT JOIN dbo.tblIncomeType AS incomeType
    ON incomeType.IncomeTypeId = sales.IncomeTypeId
WHERE sales.AssessmentId = @AssessmentId
ORDER BY sales.AssessmentSalesId;

COMMIT TRANSACTION;
