namespace ClassLibrary.ReferenceData.Models;

public sealed class TerminationReason
{
    public int TerminationReasonId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
