namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class EmployeeChangeItem
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public bool IsConfirmed { get; set; }
}
