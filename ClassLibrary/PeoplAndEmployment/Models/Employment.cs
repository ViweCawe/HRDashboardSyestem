namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class Employment
{
    public long EmploymentId { get; set; }
    public long PersonId { get; set; }
    public int CompanyId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime DateEngaged { get; set; }
    public DateTime? TerminationDate { get; set; }
    public int? TerminationReasonId { get; set; }
    public string? SourceEntityCode { get; set; }
    public long CreatedBatchId { get; set; }
}
