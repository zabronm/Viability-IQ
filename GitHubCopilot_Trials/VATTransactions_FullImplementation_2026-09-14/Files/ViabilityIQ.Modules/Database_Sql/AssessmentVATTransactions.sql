SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[dbo].[tblAssessmentVATTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblAssessmentVATTransactions]
    (
        [VATTransactionId] BIGINT IDENTITY(1,1) NOT NULL,
        [AssessmentId] BIGINT NOT NULL,
        [SourceId] BIGINT NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_SourceId] DEFAULT (0),
        [SourceType] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_SourceType] DEFAULT (N'Adjustment'),
        [Period] INT NOT NULL,
        [VATRate] DECIMAL(9,4) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_VATRate] DEFAULT (0),
        [TaxableAmount] DECIMAL(18,2) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_TaxableAmount] DEFAULT (0),
        [OutputVAT] DECIMAL(18,2) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_OutputVAT] DEFAULT (0),
        [InputVAT] DECIMAL(18,2) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_InputVAT] DEFAULT (0),
        [VATPayable] DECIMAL(18,2) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_VATPayable] DEFAULT (0),
        [Active] BIT NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_Active] DEFAULT (1),
        [Remarks] NVARCHAR(500) NULL,
        [CreatedDate] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] BIGINT NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_CreatedBy] DEFAULT (0),
        [ModifiedDate] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_ModifiedDate] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] BIGINT NOT NULL
            CONSTRAINT [DF_tblAssessmentVATTransactions_ModifiedBy] DEFAULT (0),

        CONSTRAINT [PK_tblAssessmentVATTransactions]
            PRIMARY KEY CLUSTERED ([VATTransactionId]),
        CONSTRAINT [CK_tblAssessmentVATTransactions_Period]
            CHECK ([Period] BETWEEN 1 AND 12),
        CONSTRAINT [CK_tblAssessmentVATTransactions_VATRate]
            CHECK ([VATRate] BETWEEN 0 AND 100),
        CONSTRAINT [CK_tblAssessmentVATTransactions_SourceType]
            CHECK (LEN(LTRIM(RTRIM([SourceType]))) > 0),
        CONSTRAINT [CK_tblAssessmentVATTransactions_Balance]
            CHECK ([VATPayable] = [OutputVAT] - [InputVAT])
    );
END;

IF OBJECT_ID(N'[dbo].[tblAssessments]', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE [name] = N'FK_tblAssessmentVATTransactions_tblAssessments'
   )
BEGIN
    ALTER TABLE [dbo].[tblAssessmentVATTransactions] WITH CHECK
    ADD CONSTRAINT [FK_tblAssessmentVATTransactions_tblAssessments]
        FOREIGN KEY ([AssessmentId])
        REFERENCES [dbo].[tblAssessments] ([AssessmentId]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[tblAssessmentVATTransactions]')
      AND [name] = N'IX_tblAssessmentVATTransactions_Assessment_Period_Active'
)
BEGIN
    CREATE NONCLUSTERED INDEX
        [IX_tblAssessmentVATTransactions_Assessment_Period_Active]
    ON [dbo].[tblAssessmentVATTransactions]
        ([AssessmentId], [Period], [Active])
    INCLUDE
        ([SourceType], [SourceId], [OutputVAT], [InputVAT], [VATPayable]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[tblAssessmentVATTransactions]')
      AND [name] = N'UX_tblAssessmentVATTransactions_ActiveSource'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX
        [UX_tblAssessmentVATTransactions_ActiveSource]
    ON [dbo].[tblAssessmentVATTransactions]
        ([AssessmentId], [SourceType], [SourceId], [Period])
    WHERE [Active] = 1;
END;

COMMIT TRANSACTION;

EXEC(N'
CREATE OR ALTER VIEW [dbo].[vwAssessmentVATProjection]
AS
SELECT
    [AssessmentId],
    [Period],
    SUM([OutputVAT]) AS [AdjustmentOutputVAT],
    SUM([InputVAT]) AS [AdjustmentInputVAT],
    SUM([VATPayable]) AS [AdjustmentVATPayable]
FROM [dbo].[tblAssessmentVATTransactions]
WHERE [Active] = 1
  AND [SourceType] IN (N''Adjustment'', N''Manual'')
GROUP BY [AssessmentId], [Period];
');
