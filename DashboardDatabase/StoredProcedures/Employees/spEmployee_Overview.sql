CREATE PROCEDURE [hr].[spEmployee_Overview] @EmploymentId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.PersonId, e.EmploymentId, e.EmployeeCode, COALESCE(p.DisplayName, CONCAT(p.FirstName, ' ', p.LastName)) DisplayName,
        c.Name CompanyName, e.DateEngaged, s.TerminationDate, s.SnapshotDate, es.Name StatusName,
        d.Name DepartmentName, j.Name JobTitleName, et.Name EmploymentTypeName FROM hr.Employment e JOIN hr.Person p ON p.PersonId = e.PersonId
    JOIN hr.Company c ON c.CompanyId = e.CompanyId
    OUTER APPLY (SELECT TOP (1) * FROM hr.EmployeeSnapshot ss WHERE ss.EmploymentId = e.EmploymentId  ORDER BY ss.SnapshotDate DESC) s
    LEFT JOIN hr.EmployeeStatus es ON es.EmployeeStatusId = s.EmployeeStatusId
    LEFT JOIN hr.Department d ON d.DepartmentId = s.DepartmentId
    LEFT JOIN hr.JobTitle j ON j.JobTitleId = s.JobTitleId
    LEFT JOIN hr.EmploymentType et ON et.EmploymentTypeId = s.EmploymentTypeId WHERE e.EmploymentId = @EmploymentId;
    SELECT e.PersonId, e.EmploymentId, e.EmployeeCode, COALESCE(p.DisplayName, CONCAT(p.FirstName, ' ', p.LastName)) DisplayName,
        c.Name CompanyName, e.DateEngaged, s.TerminationDate, s.SnapshotDate, es.Name StatusName,
        d.Name DepartmentName, j.Name JobTitleName, et.Name EmploymentTypeName FROM hr.Employment e JOIN hr.Person p ON p.PersonId = e.PersonId
    JOIN hr.Company c ON c.CompanyId = e.CompanyId
    OUTER APPLY (SELECT TOP (1) * FROM hr.EmployeeSnapshot ss WHERE ss.EmploymentId = e.EmploymentId  ORDER BY ss.SnapshotDate DESC) s
    LEFT JOIN hr.EmployeeStatus es ON es.EmployeeStatusId = s.EmployeeStatusId
    LEFT JOIN hr.Department d ON d.DepartmentId = s.DepartmentId
    LEFT JOIN hr.JobTitle j ON j.JobTitleId = s.JobTitleId
    LEFT JOIN hr.EmploymentType et ON et.EmploymentTypeId = s.EmploymentTypeId
    WHERE e.PersonId = (SELECT PersonId FROM hr.Employment WHERE EmploymentId = @EmploymentId) ORDER BY e.DateEngaged DESC;
    SELECT s.SnapshotDate, es.Name StatusName, d.Name DepartmentName, c.Name CostCentreName, j.Name JobTitleName, et.Name EmploymentTypeName
    FROM hr.EmployeeSnapshot s
    LEFT JOIN hr.EmployeeStatus es ON es.EmployeeStatusId = s.EmployeeStatusId
    LEFT JOIN hr.Department d ON d.DepartmentId = s.DepartmentId
    LEFT JOIN hr.CostCentre c ON c.CostCentreId = s.CostCentreId
    LEFT JOIN hr.JobTitle j ON j.JobTitleId = s.JobTitleId
    LEFT JOIN hr.EmploymentType et ON et.EmploymentTypeId = s.EmploymentTypeId
    WHERE s.EmploymentId = @EmploymentId ORDER BY s.SnapshotDate DESC;
    SELECT CASE c.ChangedField WHEN 'DepartmentId' THEN 'Department' WHEN 'CostCentreId' THEN 'Cost centre' WHEN 'JobTitleId' THEN 'Job title' WHEN 'PositionId' THEN 'Position' WHEN 'EmploymentTypeId' THEN 'Employment type' WHEN 'EmployeeStatusId' THEN 'Employee status' WHEN 'CompanyRuleId' THEN 'Company rule' WHEN 'RemunerationDefinitionId' THEN 'Remuneration definition' WHEN 'TerminationReasonId' THEN 'Termination reason' WHEN 'SourceReportsToCode' THEN 'Reports to' WHEN 'GenderCode' THEN 'Gender' WHEN 'RacialGroupCode' THEN 'Racial group' WHEN 'TerminationDate' THEN 'Termination date' ELSE c.ChangedField END FieldName,
        CASE c.ChangedField
            WHEN 'DepartmentId' THEN COALESCE((SELECT Name FROM hr.Department WHERE DepartmentId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'CostCentreId' THEN COALESCE((SELECT Name FROM hr.CostCentre WHERE CostCentreId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'JobTitleId' THEN COALESCE((SELECT Name FROM hr.JobTitle WHERE JobTitleId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'PositionId' THEN COALESCE((SELECT Name FROM hr.Position WHERE PositionId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'EmploymentTypeId' THEN COALESCE((SELECT Name FROM hr.EmploymentType WHERE EmploymentTypeId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'EmployeeStatusId' THEN COALESCE((SELECT Name FROM hr.EmployeeStatus WHERE EmployeeStatusId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'CompanyRuleId' THEN COALESCE((SELECT Name FROM hr.CompanyRule WHERE CompanyRuleId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'RemunerationDefinitionId' THEN COALESCE((SELECT Name FROM hr.RemunerationDefinition WHERE RemunerationDefinitionId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            WHEN 'TerminationReasonId' THEN COALESCE((SELECT Name FROM hr.TerminationReason WHERE TerminationReasonId = TRY_CONVERT(int, c.OldValue)), c.OldValue)
            ELSE c.OldValue END OldValue,
        CASE c.ChangedField
            WHEN 'DepartmentId' THEN COALESCE((SELECT Name FROM hr.Department WHERE DepartmentId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'CostCentreId' THEN COALESCE((SELECT Name FROM hr.CostCentre WHERE CostCentreId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'JobTitleId' THEN COALESCE((SELECT Name FROM hr.JobTitle WHERE JobTitleId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'PositionId' THEN COALESCE((SELECT Name FROM hr.Position WHERE PositionId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'EmploymentTypeId' THEN COALESCE((SELECT Name FROM hr.EmploymentType WHERE EmploymentTypeId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'EmployeeStatusId' THEN COALESCE((SELECT Name FROM hr.EmployeeStatus WHERE EmployeeStatusId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'CompanyRuleId' THEN COALESCE((SELECT Name FROM hr.CompanyRule WHERE CompanyRuleId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'RemunerationDefinitionId' THEN COALESCE((SELECT Name FROM hr.RemunerationDefinition WHERE RemunerationDefinitionId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            WHEN 'TerminationReasonId' THEN COALESCE((SELECT Name FROM hr.TerminationReason WHERE TerminationReasonId = TRY_CONVERT(int, c.NewValue)), c.NewValue)
            ELSE c.NewValue END NewValue, c.ChangeType, c.EffectiveDate, c.IsConfirmed
    FROM hr.EmploymentChange c WHERE c.EmploymentId = @EmploymentId ORDER BY c.DetectedOn DESC, c.EmploymentChangeId;
END;
