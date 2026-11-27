namespace ClassLibrary.ImportAndStaging.Models;

public sealed class ImportDashboardSummary
{
    public long EmployeeRecords { get; set; }
    public long PublishedImports { get; set; }
    public long NeedsReview { get; set; }
    public long ReadyToPublish { get; set; }
    public DateTime? LatestSnapshotDate { get; set; }
}
