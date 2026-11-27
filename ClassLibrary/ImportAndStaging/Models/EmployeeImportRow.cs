namespace ClassLibrary.ImportAndStaging.Models;

public sealed class EmployeeImportRow
{
    public long StagingRowId { get; set; }
    public long ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string RawDataJson { get; set; } = string.Empty;
    public string NormalizedDataJson { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public long? ResolvedEmploymentId { get; set; }
}
