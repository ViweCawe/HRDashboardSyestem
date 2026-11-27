CREATE PROCEDURE [hr].[spEmployeeImport_Issues] @ImportBatchId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (200) * FROM hr.ImportIssue WHERE ImportBatchId = @ImportBatchId AND ResolvedOn IS NULL ORDER BY SourceRowNumber, ImportIssueId;
END;
