namespace ClassLibrary.HoursAndLeave.Models;

public sealed class LeaveBalance
{
    public long LeaveBalanceId { get; set; }
    public long EmploymentId { get; set; }
    public int ReportingPeriodId { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal Entitlement { get; set; }
    public decimal BalanceBroughtForward { get; set; }
    public decimal AccruedThisPeriod { get; set; }
    public decimal Taken { get; set; }
    public decimal BalanceCarriedForward { get; set; }
    public string TakenBasis { get; set; } = string.Empty;
    public long SourceBatchId { get; set; }
}
