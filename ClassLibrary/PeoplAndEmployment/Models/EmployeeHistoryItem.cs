namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class EmployeeHistoryItem
{
    public DateTime SnapshotDate { get; set; }
    public string? StatusName { get; set; }
    public string? DepartmentName { get; set; }
    public string? CostCentreName { get; set; }
    public string? JobTitleName { get; set; }
    public string? EmploymentTypeName { get; set; }
}
