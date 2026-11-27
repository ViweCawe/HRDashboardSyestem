namespace ClassLibrary.PeoplAndEmployment.Models;

public sealed record EmployeeOverview(
    EmployeeListItem Employee,
    IReadOnlyList<EmployeeListItem> Engagements,
    IReadOnlyList<EmployeeHistoryItem> History,
    IReadOnlyList<EmployeeChangeItem> Changes);
