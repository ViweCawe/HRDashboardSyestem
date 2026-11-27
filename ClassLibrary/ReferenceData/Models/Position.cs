namespace ClassLibrary.ReferenceData.Models;

public sealed class Position
{
    public int PositionId { get; set; }
    public int CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
