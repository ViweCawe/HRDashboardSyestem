namespace ClassLibrary.HoursAndLeave.Models;

public sealed class HoursEntry
{
    public long HoursEntryId { get; set; }
    public long EmploymentId { get; set; }
    public int ReportingPeriodId { get; set; }
    public int HoursCategoryId { get; set; }
    public DateTime? WorkDate { get; set; }
    public int MinutesQuantity { get; set; }
    public long SourceBatchId { get; set; }
}
