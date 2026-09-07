use viabilityiq
CREATE TABLE tblAssessmentAssetDepreciationSummary (
    AssessmentAssetDepreciationSummaryId BIGINT PRIMARY KEY IDENTITY(1,1),
    AssessmentId BIGINT NOT NULL,
    MonthNumber INT NOT NULL,
    Year INT NOT NULL,
    TotalGrossValue DECIMAL(18,2) DEFAULT 0,
    TotalDepreciation DECIMAL(18,2) DEFAULT 0,
    TotalAccumulatedDepreciation DECIMAL(18,2) DEFAULT 0,
    TotalNetBookValue DECIMAL(18,2) DEFAULT 0,
    AssetCount INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    IsActive BIT DEFAULT 1,
    
    --CONSTRAINT FK_AssetDepreciationSummary_Assessment 
    --    FOREIGN KEY (AssessmentId) REFERENCES tblAssessment(AssessmentId),
    --CONSTRAINT UQ_AssetDeprSummary 
    --    UNIQUE(AssessmentId, MonthNumber, Year)
);