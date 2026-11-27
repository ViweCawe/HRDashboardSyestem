namespace ClassLibrary.ImportAndStaging.Models;

public sealed class ImportIssue
{
    public long ImportIssueId { get; set; }
    public long ImportBatchId { get; set; }
    public int? SourceRowNumber { get; set; }
    public string? SourceField { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime? ResolvedOn { get; set; }
}
