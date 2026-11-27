namespace ClassLibrary.ReferenceData.Models;

public sealed class EmployeeStatus
{
    public int EmployeeStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
