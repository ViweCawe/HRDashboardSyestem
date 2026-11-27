namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class EmploymentChange
{
    public long EmploymentChangeId { get; set; }
    public long EmploymentId { get; set; }
    public long PreviousSnapshotId { get; set; }
    public long NewSnapshotId { get; set; }
    public string ChangedField { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public DateTime DetectedOn { get; set; }
    public bool IsConfirmed { get; set; }
    public long SourceBatchId { get; set; }
}
