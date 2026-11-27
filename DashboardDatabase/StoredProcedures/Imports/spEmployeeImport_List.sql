CREATE PROCEDURE [hr].[spEmployeeImport_List]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (50) * FROM hr.ImportBatch WHERE SourceType = 'Employees' ORDER BY ImportBatchId DESC;
END;
