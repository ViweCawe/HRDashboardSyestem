namespace ClassLibrary.HoursAndLeave.Models;

public sealed class LeaveType
{
    public int LeaveTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
}
