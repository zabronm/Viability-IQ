SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tblAssessments', 'U') IS NULL
    THROW 50082, 'dbo.tblAssessments does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessments', 'OpeningShareCapital') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD OpeningShareCapital decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessments_OpeningShareCapital DEFAULT (0) WITH VALUES;
IF COL_LENGTH('dbo.tblAssessments', 'OpeningRetainedEarnings') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD OpeningRetainedEarnings decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessments_OpeningRetainedEarnings DEFAULT (0) WITH VALUES;
GO
