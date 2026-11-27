namespace ClassLibrary.HoursAndLeave.Models;

public sealed class HoursCategory
{
    public int HoursCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string QuantityBasis { get; set; } = string.Empty;
}
