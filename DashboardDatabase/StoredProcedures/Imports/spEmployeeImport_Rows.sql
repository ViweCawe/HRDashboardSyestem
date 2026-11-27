CREATE PROCEDURE [hr].[spEmployeeImport_Rows]
    @ImportBatchId bigint, @Offset int = 0, @PageSize int = 50, @Status nvarchar(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Offset < 0 OR @PageSize < 1 OR @PageSize > 100 OR (@Status IS NOT NULL AND @Status NOT IN ('Valid','Invalid','Published'))
        THROW 50001, 'Invalid preview parameters.', 1;
    -- Expose only review fields. IDs, passports and complete raw JSON stay out of the page.
    SELECT r.SourceRowNumber, r.ValidationStatus, r.ResolvedEmploymentId,
        JSON_VALUE(r.NormalizedDataJson, '$.EmployeeCode') EmployeeCode,
        COALESCE(JSON_VALUE(r.NormalizedDataJson, '$.DisplayName'), CONCAT(JSON_VALUE(r.NormalizedDataJson, '$.FirstName'), ' ', JSON_VALUE(r.NormalizedDataJson, '$.LastName'))) DisplayName,
        COALESCE(CONVERT(nvarchar(10), TRY_CONVERT(date, JSON_VALUE(r.NormalizedDataJson, '$.DateEngaged')), 103), JSON_VALUE(r.RawDataJson, '$."Date Engaged"')) DateEngagedText,
        JSON_VALUE(r.NormalizedDataJson, '$.CompanyName') CompanyName,
        JSON_VALUE(r.NormalizedDataJson, '$.DepartmentName') DepartmentName,
        JSON_VALUE(r.NormalizedDataJson, '$.JobTitleName') JobTitleName,
        JSON_VALUE(r.NormalizedDataJson, '$.EmploymentTypeName') EmploymentTypeName
    FROM hr.EmployeeImportRow r JOIN hr.ImportBatch b ON b.ImportBatchId = r.ImportBatchId
    WHERE r.ImportBatchId = @ImportBatchId AND b.SourceType = 'Employees'
      AND (@Status IS NULL OR r.ValidationStatus = @Status)
    ORDER BY r.SourceRowNumber OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
