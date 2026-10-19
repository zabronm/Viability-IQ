SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tblAssessments', 'U') IS NULL
    THROW 50061, 'dbo.tblAssessments does not exist.', 1;
GO

IF COL_LENGTH('dbo.tblAssessments', 'ManufacturingInputMethodId') IS NULL
    ALTER TABLE dbo.tblAssessments
        ADD ManufacturingInputMethodId int NOT NULL
            CONSTRAINT DF_tblAssessments_ManufacturingInputMethodId DEFAULT (1) WITH VALUES;
GO

IF OBJECT_ID('dbo.tblAssessmentManufacturingMaterials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAssessmentManufacturingMaterials
    (
        AssessmentManufacturingMaterialId bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblAssessmentManufacturingMaterials PRIMARY KEY,
        AssessmentId bigint NOT NULL,
        Description nvarchar(200) NOT NULL,
        UnitOfMeasure nvarchar(40) NOT NULL,
        OpeningQuantity decimal(19,4) NOT NULL CONSTRAINT DF_AMM_OpeningQuantity DEFAULT (0),
        OpeningValue decimal(19,4) NOT NULL CONSTRAINT DF_AMM_OpeningValue DEFAULT (0),
        IncludeVAT bit NOT NULL CONSTRAINT DF_AMM_IncludeVAT DEFAULT (0),
        VATRate decimal(9,4) NOT NULL CONSTRAINT DF_AMM_VATRate DEFAULT (0),
        TotalNoVAT decimal(19,4) NOT NULL CONSTRAINT DF_AMM_TotalNoVAT DEFAULT (0),
        TotalWithVAT decimal(19,4) NOT NULL CONSTRAINT DF_AMM_TotalWithVAT DEFAULT (0),
        Active bit NOT NULL CONSTRAINT DF_AMM_Active DEFAULT (1),
        Remarks nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL CONSTRAINT DF_AMM_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy bigint NOT NULL CONSTRAINT DF_AMM_CreatedBy DEFAULT (0),
        ModifiedDate datetime2 NOT NULL CONSTRAINT DF_AMM_ModifiedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy bigint NOT NULL CONSTRAINT DF_AMM_ModifiedBy DEFAULT (0),
        PurchaseQuantity_Month_1 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ1 DEFAULT (0),
        PurchaseQuantity_Month_2 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ2 DEFAULT (0),
        PurchaseQuantity_Month_3 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ3 DEFAULT (0),
        PurchaseQuantity_Month_4 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ4 DEFAULT (0),
        PurchaseQuantity_Month_5 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ5 DEFAULT (0),
        PurchaseQuantity_Month_6 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ6 DEFAULT (0),
        PurchaseQuantity_Month_7 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ7 DEFAULT (0),
        PurchaseQuantity_Month_8 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ8 DEFAULT (0),
        PurchaseQuantity_Month_9 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ9 DEFAULT (0),
        PurchaseQuantity_Month_10 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ10 DEFAULT (0),
        PurchaseQuantity_Month_11 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ11 DEFAULT (0),
        PurchaseQuantity_Month_12 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PQ12 DEFAULT (0),
        PurchaseValue_Month_1 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV1 DEFAULT (0),
        PurchaseValue_Month_2 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV2 DEFAULT (0),
        PurchaseValue_Month_3 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV3 DEFAULT (0),
        PurchaseValue_Month_4 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV4 DEFAULT (0),
        PurchaseValue_Month_5 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV5 DEFAULT (0),
        PurchaseValue_Month_6 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV6 DEFAULT (0),
        PurchaseValue_Month_7 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV7 DEFAULT (0),
        PurchaseValue_Month_8 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV8 DEFAULT (0),
        PurchaseValue_Month_9 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV9 DEFAULT (0),
        PurchaseValue_Month_10 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV10 DEFAULT (0),
        PurchaseValue_Month_11 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV11 DEFAULT (0),
        PurchaseValue_Month_12 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_PV12 DEFAULT (0),
        ClosingQuantity_Month_1 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ1 DEFAULT (0),
        ClosingQuantity_Month_2 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ2 DEFAULT (0),
        ClosingQuantity_Month_3 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ3 DEFAULT (0),
        ClosingQuantity_Month_4 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ4 DEFAULT (0),
        ClosingQuantity_Month_5 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ5 DEFAULT (0),
        ClosingQuantity_Month_6 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ6 DEFAULT (0),
        ClosingQuantity_Month_7 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ7 DEFAULT (0),
        ClosingQuantity_Month_8 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ8 DEFAULT (0),
        ClosingQuantity_Month_9 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ9 DEFAULT (0),
        ClosingQuantity_Month_10 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ10 DEFAULT (0),
        ClosingQuantity_Month_11 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ11 DEFAULT (0),
        ClosingQuantity_Month_12 decimal(19,4) NOT NULL CONSTRAINT DF_AMM_CQ12 DEFAULT (0)
    );
END;
GO

IF OBJECT_ID('dbo.tblAssessmentManufacturingBalances', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAssessmentManufacturingBalances
    (
        AssessmentManufacturingBalancesId bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblAssessmentManufacturingBalances PRIMARY KEY,
        AssessmentId bigint NOT NULL
            CONSTRAINT UQ_tblAssessmentManufacturingBalances_AssessmentId UNIQUE,
        OpeningWorkInProgress decimal(19,4) NOT NULL CONSTRAINT DF_AMB_OpeningWip DEFAULT (0),
        OpeningFinishedGoods decimal(19,4) NOT NULL CONSTRAINT DF_AMB_OpeningFinishedGoods DEFAULT (0),
        InventoryEstimatesConfirmed bit NOT NULL CONSTRAINT DF_AMB_Confirmed DEFAULT (0),
        Active bit NOT NULL CONSTRAINT DF_AMB_Active DEFAULT (1),
        Remarks nvarchar(max) NULL,
        CreatedDate datetime2 NOT NULL CONSTRAINT DF_AMB_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy bigint NOT NULL CONSTRAINT DF_AMB_CreatedBy DEFAULT (0),
        ModifiedDate datetime2 NOT NULL CONSTRAINT DF_AMB_ModifiedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy bigint NOT NULL CONSTRAINT DF_AMB_ModifiedBy DEFAULT (0),
        ClosingWorkInProgress_Month_1 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W1 DEFAULT (0),
        ClosingWorkInProgress_Month_2 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W2 DEFAULT (0),
        ClosingWorkInProgress_Month_3 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W3 DEFAULT (0),
        ClosingWorkInProgress_Month_4 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W4 DEFAULT (0),
        ClosingWorkInProgress_Month_5 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W5 DEFAULT (0),
        ClosingWorkInProgress_Month_6 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W6 DEFAULT (0),
        ClosingWorkInProgress_Month_7 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W7 DEFAULT (0),
        ClosingWorkInProgress_Month_8 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W8 DEFAULT (0),
        ClosingWorkInProgress_Month_9 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W9 DEFAULT (0),
        ClosingWorkInProgress_Month_10 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W10 DEFAULT (0),
        ClosingWorkInProgress_Month_11 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W11 DEFAULT (0),
        ClosingWorkInProgress_Month_12 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_W12 DEFAULT (0),
        ClosingFinishedGoods_Month_1 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F1 DEFAULT (0),
        ClosingFinishedGoods_Month_2 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F2 DEFAULT (0),
        ClosingFinishedGoods_Month_3 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F3 DEFAULT (0),
        ClosingFinishedGoods_Month_4 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F4 DEFAULT (0),
        ClosingFinishedGoods_Month_5 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F5 DEFAULT (0),
        ClosingFinishedGoods_Month_6 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F6 DEFAULT (0),
        ClosingFinishedGoods_Month_7 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F7 DEFAULT (0),
        ClosingFinishedGoods_Month_8 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F8 DEFAULT (0),
        ClosingFinishedGoods_Month_9 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F9 DEFAULT (0),
        ClosingFinishedGoods_Month_10 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F10 DEFAULT (0),
        ClosingFinishedGoods_Month_11 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F11 DEFAULT (0),
        ClosingFinishedGoods_Month_12 decimal(19,4) NOT NULL CONSTRAINT DF_AMB_F12 DEFAULT (0)
    );
END;
GO
