namespace ClassLibrary.ReferenceData.Models;

public sealed class EmploymentType
{
    public int EmploymentTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
