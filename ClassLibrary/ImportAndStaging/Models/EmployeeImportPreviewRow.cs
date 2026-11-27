namespace ClassLibrary.ImportAndStaging.Models;

public sealed class EmployeeImportPreviewRow
{
    public int SourceRowNumber { get; set; }
    public string? EmployeeCode { get; set; }
    public string? DisplayName { get; set; }
    public string? DateEngagedText { get; set; }
    public string? CompanyName { get; set; }
    public string? DepartmentName { get; set; }
    public string? JobTitleName { get; set; }
    public string? EmploymentTypeName { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public long? ResolvedEmploymentId { get; set; }
}
