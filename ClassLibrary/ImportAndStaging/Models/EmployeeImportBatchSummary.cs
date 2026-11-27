namespace ClassLibrary.ImportAndStaging.Models;

public sealed class EmployeeImportBatchSummary
{
    public long ImportBatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime SourceAsOfDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ValidRowCount { get; set; }
    public int InvalidRowCount { get; set; }
    public int PublishedRowCount { get; set; }
    public int IssueCount { get; set; }
}
