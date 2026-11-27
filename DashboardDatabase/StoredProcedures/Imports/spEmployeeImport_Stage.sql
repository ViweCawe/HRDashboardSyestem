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
        DECLARE @BatchId bigint, @ExistingStatus nvarchar(30);
        SELECT @BatchId = ImportBatchId, @ExistingStatus = Status FROM hr.ImportBatch WITH (UPDLOCK, HOLDLOCK)
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
        ELSE IF @ExistingStatus = 'Review'
        BEGIN
            -- A parser update may accept the same original file. Revalidate only
            -- unpublished review rows; retain their source JSON and staging IDs.
            DECLARE @NewRows TABLE (SourceRowNumber int PRIMARY KEY, RawDataJson nvarchar(max), NormalizedDataJson nvarchar(max), ValidationStatus nvarchar(30));
            DECLARE @NewIssues TABLE (SourceRowNumber int, SourceField nvarchar(100), RuleCode nvarchar(80), Message nvarchar(1000), Severity nvarchar(20));
            INSERT @NewRows SELECT SourceRowNumber, RawDataJson, NormalizedDataJson, ValidationStatus
                FROM OPENJSON(@RowsJson) WITH (SourceRowNumber int, RawDataJson nvarchar(max), NormalizedDataJson nvarchar(max), ValidationStatus nvarchar(30));
            INSERT @NewIssues SELECT SourceRowNumber, SourceField, RuleCode, Message, Severity
                FROM OPENJSON(@IssuesJson) WITH (SourceRowNumber int, SourceField nvarchar(100), RuleCode nvarchar(80), Message nvarchar(1000), Severity nvarchar(20));

            IF EXISTS (
                SELECT 1 FROM (SELECT * FROM hr.EmployeeImportRow WHERE ImportBatchId = @BatchId) r
                FULL JOIN @NewRows n ON n.SourceRowNumber = r.SourceRowNumber
                WHERE r.SourceRowNumber IS NULL OR n.SourceRowNumber IS NULL
                    OR r.RawDataJson COLLATE Latin1_General_100_BIN2 <> n.RawDataJson COLLATE Latin1_General_100_BIN2
                    OR n.RawDataJson IS NULL OR n.NormalizedDataJson IS NULL
                    OR ISNULL(n.ValidationStatus, '') NOT IN ('Valid','Invalid')
                    OR r.ResolvedEmploymentId IS NOT NULL OR r.ValidationStatus = 'Published')
                THROW 50001, 'Revalidation must retain the original unpublished source rows. Upload a new source file for changed data.', 1;

            IF EXISTS (SELECT 1 FROM hr.EmployeeImportRow r JOIN @NewRows n ON n.SourceRowNumber = r.SourceRowNumber
                WHERE r.ImportBatchId = @BatchId AND (r.NormalizedDataJson COLLATE Latin1_General_100_BIN2 <> n.NormalizedDataJson COLLATE Latin1_General_100_BIN2 OR r.ValidationStatus <> n.ValidationStatus))
                OR EXISTS (SELECT SourceRowNumber, SourceField, RuleCode, Message, Severity FROM @NewIssues
                    EXCEPT SELECT SourceRowNumber, SourceField, RuleCode, Message, Severity FROM hr.ImportIssue WHERE ImportBatchId = @BatchId AND ResolvedOn IS NULL)
                OR EXISTS (SELECT SourceRowNumber, SourceField, RuleCode, Message, Severity FROM hr.ImportIssue WHERE ImportBatchId = @BatchId AND ResolvedOn IS NULL
                    EXCEPT SELECT SourceRowNumber, SourceField, RuleCode, Message, Severity FROM @NewIssues)
            BEGIN
                UPDATE r SET NormalizedDataJson = n.NormalizedDataJson, ValidationStatus = n.ValidationStatus
                    FROM hr.EmployeeImportRow r JOIN @NewRows n ON n.SourceRowNumber = r.SourceRowNumber WHERE r.ImportBatchId = @BatchId;
                -- Keep earlier validation results as history; show only current issues.
                UPDATE hr.ImportIssue SET ResolvedOn = SYSUTCDATETIME() WHERE ImportBatchId = @BatchId AND ResolvedOn IS NULL;
                INSERT hr.ImportIssue (ImportBatchId, SourceRowNumber, SourceField, RuleCode, Message, Severity)
                    SELECT @BatchId, SourceRowNumber, SourceField, RuleCode, Message, Severity FROM @NewIssues;
                UPDATE hr.ImportBatch SET Status = CASE WHEN EXISTS (SELECT 1 FROM @NewIssues) THEN 'Review' ELSE 'Staged' END WHERE ImportBatchId = @BatchId;
            END;
        END;
        COMMIT;
        SELECT * FROM hr.ImportBatch WHERE ImportBatchId = @BatchId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
