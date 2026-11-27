namespace ClassLibrary.ImportAndStaging.Models;

public sealed class ImportBatch
{
    public long ImportBatchId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public DateTime SourceAsOfDate { get; set; }
    public int? ReportingPeriodId { get; set; }
    public DateTime ImportedOn { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RowCount { get; set; }
}
