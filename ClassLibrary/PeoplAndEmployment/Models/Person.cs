namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed class Person
{
    public long PersonId { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public string? PassportCountryCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
