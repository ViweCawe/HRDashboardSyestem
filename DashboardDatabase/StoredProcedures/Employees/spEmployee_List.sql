CREATE PROCEDURE [hr].[spEmployee_List]
    @AsOfDate date, @Search nvarchar(200) = NULL, @Offset int = 0, @PageSize int = 50
AS
BEGIN
    SET NOCOUNT ON;
    IF @Offset < 0 OR @PageSize < 1 OR @PageSize > 100 THROW 50001, 'Invalid paging parameters.', 1;
    SELECT e.PersonId, e.EmploymentId, e.EmployeeCode, COALESCE(p.DisplayName, CONCAT(p.FirstName, ' ', p.LastName)) DisplayName,
        c.Name CompanyName, e.DateEngaged, s.TerminationDate, s.PlannedEndDate, s.SnapshotDate, es.Name StatusName,
        d.Name DepartmentName, j.Name JobTitleName, et.Name EmploymentTypeName
    FROM hr.Employment e JOIN hr.Person p ON p.PersonId = e.PersonId
    JOIN hr.Company c ON c.CompanyId = e.CompanyId
    OUTER APPLY (SELECT TOP (1) * FROM hr.EmployeeSnapshot ss WHERE ss.EmploymentId = e.EmploymentId AND ss.SnapshotDate <= @AsOfDate ORDER BY ss.SnapshotDate DESC) s
    LEFT JOIN hr.EmployeeStatus es ON es.EmployeeStatusId = s.EmployeeStatusId
    LEFT JOIN hr.Department d ON d.DepartmentId = s.DepartmentId
    LEFT JOIN hr.JobTitle j ON j.JobTitleId = s.JobTitleId
    LEFT JOIN hr.EmploymentType et ON et.EmploymentTypeId = s.EmploymentTypeId
    WHERE e.DateEngaged <= @AsOfDate AND s.SnapshotId IS NOT NULL
      AND (@Search IS NULL OR CHARINDEX(@Search, e.EmployeeCode) > 0 OR CHARINDEX(@Search, p.DisplayName) > 0 OR CHARINDEX(@Search, CONCAT(p.FirstName, ' ', p.LastName)) > 0)
    ORDER BY e.EmployeeCode, e.DateEngaged, e.EmploymentId
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
