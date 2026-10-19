SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tblAssessments', 'U') IS NULL
    THROW 50051, 'dbo.tblAssessments does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessments', 'AssessmentModeId') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD AssessmentModeId int NOT NULL
            CONSTRAINT DF_tblAssessments_AssessmentModeId DEFAULT (1) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessments', 'CountryCode') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD CountryCode char(2) NOT NULL
            CONSTRAINT DF_tblAssessments_CountryCode DEFAULT ('ZA') WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessments', 'CurrencyCode') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD CurrencyCode char(3) NOT NULL
            CONSTRAINT DF_tblAssessments_CurrencyCode DEFAULT ('ZAR') WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessments', 'CultureName') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD CultureName nvarchar(16) NOT NULL
            CONSTRAINT DF_tblAssessments_CultureName DEFAULT (N'en-ZA') WITH VALUES;
GO

IF OBJECT_ID('dbo.tblAssessmentStock', 'U') IS NULL
    THROW 50052, 'dbo.tblAssessmentStock does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'StandardUnitCost') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD StandardUnitCost decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_StandardUnitCost DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_1') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_1 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_1 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_2') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_2 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_2 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_3') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_3 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_3 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_4') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_4 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_4 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_5') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_5 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_5 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_6') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_6 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_6 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_7') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_7 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_7 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_8') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_8 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_8 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_9') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_9 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_9 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_10') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_10 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_10 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_11') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_11 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_11 DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentStock', 'ProductionUnits_Month_12') IS NULL
    ALTER TABLE dbo.tblAssessmentStock
        ADD ProductionUnits_Month_12 decimal(19, 4) NOT NULL
            CONSTRAINT DF_tblAssessmentStock_ProductionUnits_Month_12 DEFAULT (0) WITH VALUES;
GO

IF OBJECT_ID('dbo.tblAssessmentExpenses', 'U') IS NULL
    THROW 50053, 'dbo.tblAssessmentExpenses does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessmentExpenses', 'IsDirectorWages') IS NULL
    ALTER TABLE dbo.tblAssessmentExpenses
        ADD IsDirectorWages bit NOT NULL
            CONSTRAINT DF_tblAssessmentExpenses_IsDirectorWages DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH('dbo.tblAssessmentExpenses', 'IsDirectProductionCost') IS NULL
    ALTER TABLE dbo.tblAssessmentExpenses
        ADD IsDirectProductionCost bit NOT NULL
            CONSTRAINT DF_tblAssessmentExpenses_IsDirectProductionCost DEFAULT (0) WITH VALUES;
GO
