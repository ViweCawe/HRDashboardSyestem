namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class EmployeeListItem
{
    public long PersonId { get; set; }
    public long EmploymentId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime DateEngaged { get; set; }
    public DateTime? TerminationDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? SnapshotDate { get; set; }
    public string? StatusName { get; set; }
    public string? DepartmentName { get; set; }
    public string? JobTitleName { get; set; }
    public string? EmploymentTypeName { get; set; }
}
