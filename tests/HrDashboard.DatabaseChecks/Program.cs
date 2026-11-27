using System.Security.Cryptography;
using System.Text;
using ClassLibrary.DataAccess;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Services;
using ClassLibrary.PeoplAndEmployment.Data;
using Dapper;
using Microsoft.Data.SqlClient;

// Opt-in runtime checks. Never use the development, Identity or production database.
var setting = Environment.GetEnvironmentVariable("HR_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(setting) || !new SqlConnectionStringBuilder(setting).InitialCatalog.StartsWith("HrDashboardTests_", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Set HR_TEST_CONNECTION to a disposable database named HrDashboardTests_<suffix> after publishing the SQL project.");
    return 2;
}
var factory = new SqlConnectionFactory(setting);
var imports = new EmployeeImportRepository(factory);
var employees = new EmployeeRepository(factory);
var reader = new EmployeeCsvReader();
await using var sql = await factory.OpenAsync();
var tag = Guid.NewGuid().ToString("N")[..12];
var company = "CHECK" + tag;
var companyName = "Check Company " + tag;
var id1 = "PERSON-A-" + tag;
var id2 = "PERSON-B-" + tag;
var header = "Employee Code,First Name,Last Name,Date Engaged,Company,Employee Status,ID Number,Termination Date,Termination Reason,Job Title,DEPARTMENT - Department,Employment Type";
var rows = $"C001,Alex{tag},Example,01/01/2020,{company} - {companyName},T - Terminated,{id1},31/12/2025,END - Ended,A001 - Administrator,OPS - Operations,CON - Contract\n" +
           $"C002,Alex{tag},Example,01/02/2026,{company} - {companyName},A - Active,{id1},,,A001 - Administrator,OPS - Operations,PERM - Permanent\n" +
           $"C001,Jordan{tag},Sample,01/03/2026,{company} - {companyName},A - Active,{id2},,,A001 - Administrator,OPS - Operations,CON - Contract";
var firstDate = new DateTime(2026, 7, 31);
async Task<ClassLibrary.ImportAndStaging.Models.ImportBatch> Stage(string contents, DateTime date)
{
    var preview = reader.Read(contents, date);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contents)));
    return await imports.StageAsync("database-check.csv", hash, date, "database-checks", preview);
}
async Task<int> EngagementCount() => await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company", new { company });
async Task<int> SnapshotCount() => await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.EmployeeSnapshot s JOIN hr.Employment e ON e.EmploymentId=s.EmploymentId JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company", new { company });
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    Console.WriteLine("PASS " + message);
}
async Task Rejected(long batchId, string expectedReason, string message)
{
    try { await imports.PublishAsync(batchId); }
    catch (SqlException ex) when (ex.Number == 50001 && ex.Message.Contains(expectedReason, StringComparison.OrdinalIgnoreCase)) { Console.WriteLine("PASS " + message); return; }
    throw new InvalidOperationException(message);
}
var first = await Stage(header + "\n" + rows, firstDate);
Check(first.Status == "Staged", "Valid employee file stages successfully");
var stagedSummary = await imports.GetBatchAsync(first.ImportBatchId);
Check(stagedSummary is { RowCount: 3, ValidRowCount: 3, IssueCount: 0 }, "Batch review summarises validated employee records");
var previewRows = await imports.GetRowsAsync(first.ImportBatchId, 0, 50, "Valid");
Check(previewRows.Count == 3 && previewRows[0].SourceRowNumber == 2, "Batch preview returns source rows in order");
Check((await imports.GetRowsAsync(first.ImportBatchId, 1, 1)).Count == 1, "Batch preview supports paging");
await imports.PublishAsync(first.ImportBatchId);
Check((await imports.GetBatchAsync(first.ImportBatchId))?.PublishedRowCount == 3, "Published batch review shows published records");
Check(await EngagementCount() == 3, "New codes create separate engagements and recycled codes do not collapse them");
var people = await sql.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT e.PersonId) FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company", new { company });
Check(people == 2, "Re-employed person keeps one PersonId while the recycled-code holder has a different PersonId");
Check(await SnapshotCount() == 3, "Baseline contains one snapshot per engagement");
var duplicate = await Stage(header + "\n" + rows, firstDate);
Check(duplicate.ImportBatchId == first.ImportBatchId, "Exact file/date reimport returns the original batch");
await imports.PublishAsync(duplicate.ImportBatchId);
Check(await EngagementCount() == 3 && await SnapshotCount() == 3, "Republishing creates no duplicate engagements or snapshots");
var laterRows = rows.Replace($"C002,Alex{tag},Example,01/02/2026,{company} - {companyName},A - Active,{id1},,,A001 - Administrator", $"C002,Alex{tag},Example,01/02/2026,{company} - {companyName},A - Active,{id1},,,M001 - Manager");
var later = await Stage(header + "\n" + laterRows, firstDate.AddMonths(1));
await imports.PublishAsync(later.ImportBatchId);
var employmentId = await sql.ExecuteScalarAsync<long>("SELECT e.EmploymentId FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company AND e.EmployeeCode='C002'", new { company });
var overview = await employees.GetOverviewAsync(employmentId);
Check(overview is { Engagements.Count: 2, History.Count: 2 }, "Overview links both engagements and retains both snapshots");
Check(overview!.Changes.Any(c => c.FieldName == "Job title" && c.OldValue == "Administrator" && c.NewValue == "Manager" && !c.IsConfirmed && c.EffectiveDate is null), "Job-title change has readable values and is not labelled a confirmed promotion");
var list = await employees.GetAsync(firstDate, "Alex" + tag);
var asOfRecords = list.Where(e => e.CompanyName == companyName).ToArray();
Check(asOfRecords.Length == 2 && asOfRecords.Any(e => e.EmploymentId == employmentId) && asOfRecords.All(e => e.JobTitleName == "Administrator"), "As-of query returns both engagements using the earlier job title");
var beforePeople = await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Person");
var overlap = header + $"\nC001,Another,Person,01/10/2026,{company} - {companyName},A - Active,PERSON-C-{tag},,,A001 - Administrator,OPS - Operations,CON - Contract";
var bad = await Stage(overlap, new DateTime(2026, 10, 31));
await Rejected(bad.ImportBatchId, "overlaps an existing engagement", "Overlapping recycled code is rejected");
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Person") == beforePeople && await EngagementCount() == 3, "Failed publication rolls back newly inserted people and engagements");
var correction = await Stage(header + "\n" + laterRows.Replace("Alex", "Changed"), firstDate.AddMonths(1));
await Rejected(correction.ImportBatchId, "already has a snapshot for that date", "Different file cannot silently replace an existing same-date snapshot");
Check(await sql.ExecuteScalarAsync<string>("SELECT FirstName FROM hr.Person WHERE NationalId=@id1", new { id1 }) == "Alex" + tag && await SnapshotCount() == 6, "Rejected correction rolls back personal details and preserves all snapshots");
var older = await Stage(header + "\n" + rows, firstDate.AddDays(-1));
await Rejected(older.ImportBatchId, "older export cannot overwrite", "Older export cannot overwrite newer history");
var invalid = await Stage(header + $"\nBAD,Missing,Identity,01/01/2026,{company} - {companyName},A - Active,,,,A001 - Administrator,OPS - Operations,CON - Contract", firstDate);
Check(invalid.Status == "Review" && (await imports.GetIssuesAsync(invalid.ImportBatchId)).Count > 0, "Invalid source rows remain staged with review issues");
await Rejected(invalid.ImportBatchId, "Resolve the source validation errors", "Validation errors block publication");
var dashboard = await imports.GetDashboardAsync();
Check(dashboard.EmployeeRecords >= 3 && dashboard.PublishedImports >= 2, "Workspace totals reflect persisted records and imports");
Check((await imports.GetBatchAsync(invalid.ImportBatchId)) is { InvalidRowCount: 1, IssueCount: > 0 }, "Invalid batch review reports blocked rows and issues");
Console.WriteLine("Database behaviour checks completed. Drop the disposable database after review.");
return 0;
