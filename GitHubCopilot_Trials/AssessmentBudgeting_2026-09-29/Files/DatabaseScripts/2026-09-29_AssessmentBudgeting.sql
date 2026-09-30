SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.tblAssessmentBudget', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAssessmentBudget
    (
        BudgetId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblAssessmentBudget PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        AssessmentId BIGINT NOT NULL,
        BudgetName NVARCHAR(150) NOT NULL,
        Status VARCHAR(20) NOT NULL
            CONSTRAINT DF_tblAssessmentBudget_Status DEFAULT ('Draft'),
        Remarks NVARCHAR(1000) NULL,
        OpeningBank DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_tblAssessmentBudget_OpeningBank DEFAULT (0),
        ProjectionCalculatedAtUtc DATETIME2(7) NOT NULL,
        ProjectionImportedAtUtc DATETIME2(7) NOT NULL,
        SubmittedDate DATETIME2(7) NULL,
        SubmittedBy BIGINT NULL,
        ApprovedDate DATETIME2(7) NULL,
        ApprovedBy BIGINT NULL,
        Active BIT NOT NULL
            CONSTRAINT DF_tblAssessmentBudget_Active DEFAULT (1),
        CreatedDate DATETIME2(7) NOT NULL
            CONSTRAINT DF_tblAssessmentBudget_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy BIGINT NOT NULL,
        ModifiedDate DATETIME2(7) NOT NULL
            CONSTRAINT DF_tblAssessmentBudget_ModifiedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy BIGINT NOT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAssessmentBudget_Assessment
            FOREIGN KEY (AssessmentId) REFERENCES dbo.tblAssessments(AssessmentId),
        CONSTRAINT CK_tblAssessmentBudget_Status
            CHECK (Status IN ('Draft', 'Submitted', 'Approved'))
    );

    CREATE UNIQUE INDEX UX_tblAssessmentBudget_Assessment_Active
        ON dbo.tblAssessmentBudget(TenantId, AssessmentId)
        WHERE Active = 1;

    CREATE INDEX IX_tblAssessmentBudget_Assessment
        ON dbo.tblAssessmentBudget(AssessmentId, TenantId, Status)
        INCLUDE (BudgetName, ModifiedDate);
END;

IF OBJECT_ID(N'dbo.tblAssessmentBudgetLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAssessmentBudgetLine
    (
        BudgetLineId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblAssessmentBudgetLine PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        AssessmentId BIGINT NOT NULL,
        BudgetId BIGINT NOT NULL,
        SectionCode VARCHAR(40) NOT NULL,
        SectionName NVARCHAR(100) NOT NULL,
        LineCode VARCHAR(50) NOT NULL,
        LineName NVARCHAR(150) NOT NULL,
        DisplayOrder INT NOT NULL,
        IsCalculated BIT NOT NULL
            CONSTRAINT DF_tblAssessmentBudgetLine_IsCalculated DEFAULT (0),
        ProjectedMonth1 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth2 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth3 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth4 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth5 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth6 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth7 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth8 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth9 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth10 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth11 DECIMAL(19,4) NOT NULL DEFAULT (0),
        ProjectedMonth12 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth1 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth2 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth3 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth4 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth5 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth6 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth7 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth8 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth9 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth10 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth11 DECIMAL(19,4) NOT NULL DEFAULT (0),
        BudgetMonth12 DECIMAL(19,4) NOT NULL DEFAULT (0),
        Active BIT NOT NULL
            CONSTRAINT DF_tblAssessmentBudgetLine_Active DEFAULT (1),
        CreatedDate DATETIME2(7) NOT NULL
            CONSTRAINT DF_tblAssessmentBudgetLine_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy BIGINT NOT NULL,
        ModifiedDate DATETIME2(7) NOT NULL
            CONSTRAINT DF_tblAssessmentBudgetLine_ModifiedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy BIGINT NOT NULL,
        CONSTRAINT FK_tblAssessmentBudgetLine_Budget
            FOREIGN KEY (BudgetId) REFERENCES dbo.tblAssessmentBudget(BudgetId),
        CONSTRAINT FK_tblAssessmentBudgetLine_Assessment
            FOREIGN KEY (AssessmentId) REFERENCES dbo.tblAssessments(AssessmentId),
        CONSTRAINT UQ_tblAssessmentBudgetLine_Code
            UNIQUE (BudgetId, LineCode)
    );

    CREATE INDEX IX_tblAssessmentBudgetLine_Budget
        ON dbo.tblAssessmentBudgetLine(BudgetId, DisplayOrder)
        INCLUDE (AssessmentId, TenantId, SectionCode, IsCalculated);
END;

IF OBJECT_ID(N'dbo.tblAssessmentBudgetStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAssessmentBudgetStatusHistory
    (
        BudgetStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tblAssessmentBudgetStatusHistory PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        AssessmentId BIGINT NOT NULL,
        BudgetId BIGINT NOT NULL,
        PreviousStatus VARCHAR(20) NULL,
        NewStatus VARCHAR(20) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        ChangedDate DATETIME2(7) NOT NULL
            CONSTRAINT DF_tblAssessmentBudgetStatusHistory_ChangedDate
            DEFAULT (SYSUTCDATETIME()),
        ChangedBy BIGINT NOT NULL,
        CONSTRAINT FK_tblAssessmentBudgetStatusHistory_Budget
            FOREIGN KEY (BudgetId) REFERENCES dbo.tblAssessmentBudget(BudgetId),
        CONSTRAINT FK_tblAssessmentBudgetStatusHistory_Assessment
            FOREIGN KEY (AssessmentId) REFERENCES dbo.tblAssessments(AssessmentId)
    );

    CREATE INDEX IX_tblAssessmentBudgetStatusHistory_Budget
        ON dbo.tblAssessmentBudgetStatusHistory(BudgetId, ChangedDate DESC);
END;

COMMIT TRANSACTION;
