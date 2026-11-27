CREATE PROCEDURE [hr].[spEmployeeImport_Get] @ImportBatchId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.ImportBatchId, b.FileName, b.SourceAsOfDate, b.Status, b.[RowCount],
        (SELECT COUNT(*) FROM hr.EmployeeImportRow r WHERE r.ImportBatchId = b.ImportBatchId AND r.ValidationStatus IN ('Valid','Published')) ValidRowCount,
        (SELECT COUNT(*) FROM hr.EmployeeImportRow r WHERE r.ImportBatchId = b.ImportBatchId AND r.ValidationStatus = 'Invalid') InvalidRowCount,
        (SELECT COUNT(*) FROM hr.EmployeeImportRow r WHERE r.ImportBatchId = b.ImportBatchId AND r.ValidationStatus = 'Published') PublishedRowCount,
        (SELECT COUNT(*) FROM hr.ImportIssue i WHERE i.ImportBatchId = b.ImportBatchId AND i.ResolvedOn IS NULL) IssueCount
    FROM hr.ImportBatch b WHERE b.ImportBatchId = @ImportBatchId AND b.SourceType = 'Employees';
END;
