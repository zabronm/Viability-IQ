IF COL_LENGTH('dbo.tblAssessments', 'CompletedDate') IS NULL
    ALTER TABLE dbo.tblAssessments ADD CompletedDate datetime2 NULL;

IF COL_LENGTH('dbo.tblAssessments', 'CompletedBy') IS NULL
    ALTER TABLE dbo.tblAssessments ADD CompletedBy bigint NULL;

IF COL_LENGTH('dbo.tblAssessments', 'ReopenedDate') IS NULL
    ALTER TABLE dbo.tblAssessments ADD ReopenedDate datetime2 NULL;

IF COL_LENGTH('dbo.tblAssessments', 'ReopenedBy') IS NULL
    ALTER TABLE dbo.tblAssessments ADD ReopenedBy bigint NULL;

UPDATE dbo.tblAssessments
SET CompletedDate = ModifiedDate,
    CompletedBy = ModifiedBy
WHERE StatusId = 4
  AND CompletedDate IS NULL;
