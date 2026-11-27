namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class EmployeeSnapshot
{
    public long SnapshotId { get; set; }
    public long EmploymentId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public int? DepartmentId { get; set; }
    public int? CostCentreId { get; set; }
    public int? JobTitleId { get; set; }
    public int? PositionId { get; set; }
    public int? EmploymentTypeId { get; set; }
    public int EmployeeStatusId { get; set; }
    public int? CompanyRuleId { get; set; }
    public int? RemunerationDefinitionId { get; set; }
    public long? ManagerEmploymentId { get; set; }
    public string? SourceReportsToCode { get; set; }
    public string? GenderCode { get; set; }
    public string? RacialGroupCode { get; set; }
    public string? Disability { get; set; }
    public DateTime? TerminationDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? TerminationReasonId { get; set; }
    public long SourceBatchId { get; set; }
}
