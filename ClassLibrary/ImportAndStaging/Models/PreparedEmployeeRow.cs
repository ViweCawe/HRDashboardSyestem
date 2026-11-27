namespace ClassLibrary.ImportAndStaging.Models;

public sealed record PreparedEmployeeRow(int SourceRowNumber, string RawDataJson, string NormalizedDataJson, string ValidationStatus);
public sealed record EmployeeImportPreview(IReadOnlyList<PreparedEmployeeRow> Rows, IReadOnlyList<ImportIssue> Issues);
