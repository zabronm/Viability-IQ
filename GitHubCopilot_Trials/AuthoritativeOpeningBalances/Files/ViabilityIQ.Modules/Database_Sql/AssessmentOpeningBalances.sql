SET XACT_ABORT ON;

IF COL_LENGTH('dbo.tblAssessments', 'OpeningOtherCurrentAssets') IS NULL
    ALTER TABLE dbo.tblAssessments ADD OpeningOtherCurrentAssets DECIMAL(19,4) NOT NULL
        CONSTRAINT DF_tblAssessments_OpeningOtherCurrentAssets DEFAULT (0);

IF COL_LENGTH('dbo.tblAssessments', 'OpeningOtherCurrentLiabilities') IS NULL
    ALTER TABLE dbo.tblAssessments ADD OpeningOtherCurrentLiabilities DECIMAL(19,4) NOT NULL
        CONSTRAINT DF_tblAssessments_OpeningOtherCurrentLiabilities DEFAULT (0);

IF COL_LENGTH('dbo.tblAssessments', 'OpeningVatPayable') IS NULL
    ALTER TABLE dbo.tblAssessments ADD OpeningVatPayable DECIMAL(19,4) NOT NULL
        CONSTRAINT DF_tblAssessments_OpeningVatPayable DEFAULT (0);

IF COL_LENGTH('dbo.tblAssessments', 'OpeningVatRefundable') IS NULL
    ALTER TABLE dbo.tblAssessments ADD OpeningVatRefundable DECIMAL(19,4) NOT NULL
        CONSTRAINT DF_tblAssessments_OpeningVatRefundable DEFAULT (0);
