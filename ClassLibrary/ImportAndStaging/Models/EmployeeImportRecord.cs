namespace ClassLibrary.ImportAndStaging.Models;

public sealed class EmployeeImportRecord
{
    public string? EmployeeCode { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public string? PassportCountryCode { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? SourceEntityCode { get; set; }
    public string? CompanyCode { get; set; }
    public string? CompanyName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? CostCentreCode { get; set; }
    public string? CostCentreName { get; set; }
    public string? JobTitleCode { get; set; }
    public string? JobTitleName { get; set; }
    public string? PositionCode { get; set; }
    public string? PositionName { get; set; }
    public string? EmployeeStatusCode { get; set; }
    public string? EmployeeStatusName { get; set; }
    public string? EmploymentTypeCode { get; set; }
    public string? EmploymentTypeName { get; set; }
    public string? CompanyRuleCode { get; set; }
    public string? CompanyRuleName { get; set; }
    public string? RemunerationDefinitionCode { get; set; }
    public string? RemunerationDefinitionName { get; set; }
    public string? TerminationReasonCode { get; set; }
    public string? TerminationReasonName { get; set; }
    public string? SourceReportsToCode { get; set; }
    public string? GenderCode { get; set; }
    public string? RacialGroupCode { get; set; }
    public string? Disability { get; set; }
    public DateTime? DateEngaged { get; set; }
    public DateTime? TerminationDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? TransferDate { get; set; }
}
