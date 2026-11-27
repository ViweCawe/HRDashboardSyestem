namespace ClassLibrary.HoursAndLeave.Models;

public sealed class ReportingPeriod
{
    public int ReportingPeriodId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Grain { get; set; } = string.Empty;
}
