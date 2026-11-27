CREATE PROCEDURE [hr].[spEmployeeImport_Stage]
    @FileName nvarchar(260), @FileHash char(64), @AsOfDate date,
    @ImportedBy nvarchar(200), @RowsJson nvarchar(max), @IssuesJson nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF ISJSON(@RowsJson) <> 1 OR ISJSON(@IssuesJson) <> 1
        THROW 50001, 'Invalid import payload.', 1;
    IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RowsJson))
        THROW 50001, 'An empty export cannot be staged.', 1;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @BatchId bigint;
        SELECT @BatchId = ImportBatchId FROM hr.ImportBatch WITH (UPDLOCK, HOLDLOCK)
        WHERE SourceType = 'Employees' AND SourceAsOfDate = @AsOfDate AND FileHash = @FileHash;
        IF @BatchId IS NULL
        BEGIN
            INSERT hr.ImportBatch (SourceSystem, SourceType, FileName, FileHash, SourceAsOfDate, ImportedOn, ImportedBy, Status, [RowCount])
            SELECT 'Sage', 'Employees', @FileName, @FileHash, @AsOfDate, SYSUTCDATETIME(), @ImportedBy,
                CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@IssuesJson)) THEN 'Review' ELSE 'Staged' END,
                (SELECT COUNT(*) FROM OPENJSON(@RowsJson));
            SET @BatchId = SCOPE_IDENTITY();
            INSERT hr.EmployeeImportRow (ImportBatchId, SourceRowNumber, RawDataJson, NormalizedDataJson, ValidationStatus)
            SELECT @BatchId, SourceRowNumber, RawDataJson, NormalizedDataJson, ValidationStatus
            FROM OPENJSON(@RowsJson) WITH (SourceRowNumber int, RawDataJson nvarchar(max), NormalizedDataJson nvarchar(max), ValidationStatus nvarchar(30));
            INSERT hr.ImportIssue (ImportBatchId, SourceRowNumber, SourceField, RuleCode, Message, Severity)
            SELECT @BatchId, SourceRowNumber, SourceField, RuleCode, Message, Severity
            FROM OPENJSON(@IssuesJson) WITH (SourceRowNumber int, SourceField nvarchar(100), RuleCode nvarchar(80), Message nvarchar(1000), Severity nvarchar(20));
        END;
        COMMIT;
        SELECT * FROM hr.ImportBatch WHERE ImportBatchId = @BatchId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
