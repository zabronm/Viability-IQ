SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.tblAssessmentProjectionAssumptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAssessmentProjectionAssumptions
    (
        AssessmentProjectionAssumptionsId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblAssessmentProjectionAssumptions PRIMARY KEY,
        AssessmentId BIGINT NOT NULL,
        SalesGrowthRate DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_SalesGrowthRate DEFAULT (0),
        SalesGrowthStartMonth INT NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_SalesGrowthStartMonth DEFAULT (1),
        CashSalesPercentage DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_CashSalesPercentage DEFAULT (0),
        BadDebtRate DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_BadDebtRate DEFAULT (0),
        CostOfSalesPercentage DECIMAL(9,4) NULL,
        MinimumClosingStockPercentage DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_MinimumClosingStockPercentage DEFAULT (0),
        ExpenseIncreaseRate DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_ExpenseIncreaseRate DEFAULT (0),
        ExpenseIncreaseStartMonth INT NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_ExpenseIncreaseStartMonth DEFAULT (1),
        ContingencyRate DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_ContingencyRate DEFAULT (0),
        VatPaymentFrequencyMonths INT NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_VatFrequency DEFAULT (2),
        DirectorWageIncreaseRate DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_DirectorWageIncrease DEFAULT (0),
        MinimumCashBalance DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_MinimumCash DEFAULT (0),
        IsConfirmed BIT NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_IsConfirmed DEFAULT (0),
        ConfirmedDate DATETIME2(0) NULL,
        ConfirmedBy NVARCHAR(256) NULL,
        Active BIT NOT NULL
            CONSTRAINT DF_AssessmentProjectionAssumptions_Active DEFAULT (1),
        Remarks NVARCHAR(1000) NULL,
        CreatedBy BIGINT NOT NULL CONSTRAINT DF_tblAssessmentProjectionAssumptions_CreatedBy DEFAULT (0),
        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_tblAssessmentProjectionAssumptions_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy BIGINT NOT NULL CONSTRAINT DF_tblAssessmentProjectionAssumptions_ModifiedBy DEFAULT (0),
        ModifiedDate DATETIME2(0) NOT NULL CONSTRAINT DF_tblAssessmentProjectionAssumptions_ModifiedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AssessmentProjectionAssumptions_Assessment
            FOREIGN KEY (AssessmentId) REFERENCES dbo.tblAssessments(AssessmentId),
        CONSTRAINT CK_AssessmentProjectionAssumptions_SalesStart
            CHECK (SalesGrowthStartMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_AssessmentProjectionAssumptions_ExpenseStart
            CHECK (ExpenseIncreaseStartMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_AssessmentProjectionAssumptions_VatFrequency
            CHECK (VatPaymentFrequencyMonths IN (1, 2)),
        CONSTRAINT CK_AssessmentProjectionAssumptions_Percentages
            CHECK
            (
                SalesGrowthRate BETWEEN -100 AND 1000
                AND CashSalesPercentage BETWEEN 0 AND 100
                AND BadDebtRate BETWEEN 0 AND 100
                AND (CostOfSalesPercentage IS NULL OR CostOfSalesPercentage BETWEEN 0 AND 100)
                AND MinimumClosingStockPercentage BETWEEN 0 AND 100
                AND ExpenseIncreaseRate BETWEEN -100 AND 1000
                AND ContingencyRate BETWEEN 0 AND 100
                AND DirectorWageIncreaseRate BETWEEN -100 AND 1000
            )
    );

    CREATE UNIQUE INDEX UX_AssessmentProjectionAssumptions_Assessment
        ON dbo.tblAssessmentProjectionAssumptions (AssessmentId)
        WHERE Active = 1;
END;

INSERT INTO dbo.tblAssessmentProjectionAssumptions
(
    AssessmentId,
    SalesGrowthRate,
    SalesGrowthStartMonth,
    CashSalesPercentage,
    BadDebtRate,
    MinimumClosingStockPercentage,
    ExpenseIncreaseRate,
    ExpenseIncreaseStartMonth,
    ContingencyRate,
    VatPaymentFrequencyMonths,
    DirectorWageIncreaseRate,
    MinimumCashBalance,
    IsConfirmed,
    Active,
    CreatedDate
)
SELECT
    assessment.AssessmentId,
    0,
    1,
    0,
    COALESCE(profile.BadDebtPercentage, 0),
    0,
    0,
    1,
    0,
    2,
    0,
    0,
    0,
    1,
    SYSUTCDATETIME()
FROM dbo.tblAssessments assessment
OUTER APPLY
(
    SELECT TOP (1) debtorProfile.BadDebtPercentage
    FROM dbo.tblAssessmentDebtorsCreditorsProfile debtorProfile
    WHERE debtorProfile.AssessmentId = assessment.AssessmentId
      AND debtorProfile.Active = 1
    ORDER BY debtorProfile.DebtorsCreditorsProfileId DESC
) profile
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.tblAssessmentProjectionAssumptions existing
    WHERE existing.AssessmentId = assessment.AssessmentId
      AND existing.Active = 1
);
