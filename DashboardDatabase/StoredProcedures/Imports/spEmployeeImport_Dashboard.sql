CREATE PROCEDURE [hr].[spEmployeeImport_Dashboard]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT (SELECT COUNT_BIG(*) FROM hr.Employment) EmployeeRecords,
        COUNT_BIG(CASE WHEN b.Status = 'Published' THEN 1 END) PublishedImports,
        COUNT_BIG(CASE WHEN b.Status = 'Review' THEN 1 END) NeedsReview,
        COUNT_BIG(CASE WHEN b.Status = 'Staged' THEN 1 END) ReadyToPublish,
        MAX(CASE WHEN b.Status = 'Published' THEN b.SourceAsOfDate END) LatestSnapshotDate
    FROM hr.ImportBatch b WHERE b.SourceType = 'Employees';
END;
