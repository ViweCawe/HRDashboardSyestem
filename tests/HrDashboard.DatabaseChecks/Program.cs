using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClassLibrary.DataAccess;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Services;
using ClassLibrary.ImportAndStaging.Models;
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

// Simulate the earlier parser rejecting yyyy/MM/dd, then revalidate the exact
// same bytes after upgrading. No private export is used in these fixtures.
EmployeeImportPreview LegacyDateResult(EmployeeImportPreview current)
{
    var source = current.Rows.Single();
    var record = JsonSerializer.Deserialize<EmployeeImportRecord>(source.NormalizedDataJson)!;
    record.DateEngaged = null;
    return new([source with { NormalizedDataJson = JsonSerializer.Serialize(record), ValidationStatus = "Invalid" }],
        [..current.Issues,
            new() { SourceRowNumber = 2, SourceField = "Date Engaged", RuleCode = "InvalidDate", Message = "Legacy date format issue", Severity = "Error" },
            new() { SourceRowNumber = 2, SourceField = "Date Engaged", RuleCode = "Required", Message = "Legacy duplicate required issue", Severity = "Error" }]);
}
var yearFirstCsv = header + $"\nDATE01,YearFirst,Example,2026/07/01,{company} - {companyName},A - Active,YEAR-ID-{tag},,,A001 - Administrator,OPS - Operations,CON - Contract";
var currentDates = reader.Read(yearFirstCsv, firstDate);
var dateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(yearFirstCsv)));
var legacy = await imports.StageAsync("year-first.csv", dateHash, firstDate, "database-checks", LegacyDateResult(currentDates));
var sourceRowId = await sql.ExecuteScalarAsync<long>("SELECT StagingRowId FROM hr.EmployeeImportRow WHERE ImportBatchId=@id", new { id = legacy.ImportBatchId });
Check(legacy.Status == "Review" && (await imports.GetIssuesAsync(legacy.ImportBatchId)).Count == 2, "Earlier date-format failure remains an unpublished review batch");
var revalidated = await imports.StageAsync("year-first.csv", dateHash, firstDate, "database-checks", currentDates);
Check(revalidated.ImportBatchId == legacy.ImportBatchId && revalidated.Status == "Staged" && (await imports.GetIssuesAsync(legacy.ImportBatchId)).Count == 0, "Same file revalidation clears obsolete date errors without creating another batch");
Check(await sql.ExecuteScalarAsync<long>("SELECT StagingRowId FROM hr.EmployeeImportRow WHERE ImportBatchId=@id AND RawDataJson=@raw", new { id = legacy.ImportBatchId, raw = currentDates.Rows[0].RawDataJson }) == sourceRowId
    && await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.ImportIssue WHERE ImportBatchId=@id AND ResolvedOn IS NOT NULL", new { id = legacy.ImportBatchId }) == 2,
    "Revalidation preserves the original source row and earlier issue history");
await imports.PublishAsync(revalidated.ImportBatchId);
await imports.StageAsync("year-first.csv", dateHash, firstDate, "database-checks", LegacyDateResult(currentDates));
Check((await imports.GetBatchAsync(revalidated.ImportBatchId)) is { Status: "Published", PublishedRowCount: 1, IssueCount: 0 }, "Revalidation never changes published rows even if another parser supplies different results");

var partialCsv = yearFirstCsv.Replace("DATE01", "DATE02").Replace("YEAR-ID-" + tag, "");
var partialDates = reader.Read(partialCsv, firstDate);
var partialHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partialCsv)));
var partial = await imports.StageAsync("remaining-issue.csv", partialHash, firstDate, "database-checks", LegacyDateResult(partialDates));
var revisedPartial = await imports.StageAsync("remaining-issue.csv", partialHash, firstDate, "database-checks", partialDates);
var remaining = await imports.GetIssuesAsync(partial.ImportBatchId);
Check(revisedPartial.Status == "Review" && remaining.Count == 1 && remaining[0].RuleCode == "MissingIdentity", "Revalidation removes obsolete date errors while preserving other publication blockers");
var issueCount = await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.ImportIssue WHERE ImportBatchId=@id", new { id = partial.ImportBatchId });
await imports.StageAsync("remaining-issue.csv", partialHash, firstDate, "database-checks", partialDates);
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.ImportIssue WHERE ImportBatchId=@id", new { id = partial.ImportBatchId }) == issueCount, "Identical review results do not duplicate validation history");
try
{
    var tampered = partialDates.Rows[0] with { RawDataJson = partialDates.Rows[0].RawDataJson.Replace("YearFirst", "Changed") };
    await imports.StageAsync("remaining-issue.csv", partialHash, firstDate, "database-checks", new([tampered], partialDates.Issues));
    throw new InvalidOperationException("A revalidation changed the original source rows.");
}
catch (SqlException ex) when (ex.Number == 50001 && ex.Message.Contains("retain the original unpublished source rows", StringComparison.Ordinal))
{
    Check((await imports.GetIssuesAsync(partial.ImportBatchId)).Count == 1, "Changed raw rows cannot replace a review batch under the same file hash");
}
// HR supplies planned contract ends while status remains Active. Unused identity
// fields contain N/A; these values must never be stored as person match keys.
var planHeader = header + ",Passport No,Passport Country Code";
var planId = "PLAN-ID-" + tag;
var planPassport = "PLAN-PASS-" + tag;
var planRow = $"CON01,Plan{tag},Example,2026/07/01,{company} - {companyName},A - Active,{planId},2026/09/30,END - Contract end,A001 - Administrator,OPS - Operations,CON - Contract,N/A,N/A";
var passportRow = $"PASS01,Plan{tag},Passport,2026/07/01,{company} - {companyName},A - Active,N/A,2026/09/30,END - Contract end,A001 - Administrator,OPS - Operations,CON - Contract,{planPassport},ZA";
var planned = await Stage(planHeader + "\n" + planRow + "\n" + passportRow, firstDate);
await imports.PublishAsync(planned.ImportBatchId);
Check((await imports.GetBatchAsync(planned.ImportBatchId))?.PublishedRowCount == 2, "Active employees with planned end dates and unused N/A identities publish successfully");
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Person WHERE NationalId=@planId AND PassportNumber IS NULL AND PassportCountryCode IS NULL", new { planId }) == 1,
    "An ID-based person never stores unused passport N/A as a match key");
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Person WHERE PassportNumber=@planPassport AND PassportCountryCode='ZA' AND NationalId IS NULL", new { planPassport }) == 1,
    "A passport-based person never stores unused ID N/A as a match key");
var planRecords = (await employees.GetAsync(firstDate, "Plan" + tag)).Where(e => e.CompanyName == companyName).ToArray();
Check(planRecords.Length == 2 && planRecords.All(e => e.StatusName == "Active" && e.PlannedEndDate == new DateTime(2026, 9, 30) && e.TerminationDate is null),
    "As-of records retain Active status with planned dates and no actual termination");
var earlyRecycle = header + $"\nCON01,Another,Holder,2026/10/01,{company} - {companyName},A - Active,EARLY-{tag},,,A001 - Administrator,OPS - Operations,CON - Contract";
var early = await Stage(earlyRecycle, new DateTime(2026, 10, 31));
await Rejected(early.ImportBatchId, "overlaps an existing engagement", "A planned end does not release an active employee code for recycling");
var renewal = await Stage(planHeader + "\n" + planRow.Replace("2026/09/30", "2026/11/30"), firstDate.AddMonths(1));
await imports.PublishAsync(renewal.ImportBatchId);
var planEmploymentId = planRecords.Single(e => e.EmployeeCode == "CON01").EmploymentId;
var renewed = await employees.GetOverviewAsync(planEmploymentId);
Check(renewed is { History.Count: 2 } && renewed.Employee.PlannedEndDate == new DateTime(2026, 11, 30)
    && renewed.Changes.Any(c => c.FieldName == "Planned end date" && c.OldValue == "2026-09-30" && c.NewValue == "2026-11-30"),
    "An extended planned end has its own readable change and retains both snapshots");
var terminatedPlan = await Stage(planHeader + "\n" + planRow.Replace("A - Active", "T - Terminated").Replace("2026/09/30", "2026/11/30"), new DateTime(2026, 11, 30));
await imports.PublishAsync(terminatedPlan.ImportBatchId);
var ended = await employees.GetOverviewAsync(planEmploymentId);
Check(ended is { History.Count: 3 } && ended.Employee.StatusName == "Terminated" && ended.Employee.PlannedEndDate is null
    && ended.Employee.TerminationDate == new DateTime(2026, 11, 30), "A subsequent Sage termination records the actual end and preserves earlier planned history");
Check((await employees.GetAsync(firstDate, "Plan" + tag)).Any(e => e.EmploymentId == planEmploymentId && e.StatusName == "Active"
    && e.PlannedEndDate == new DateTime(2026, 9, 30) && e.TerminationDate is null), "Earlier as-of reporting keeps the original active planned-end snapshot after termination");
// ESS_APPR approvers are the only HR-approved missing-document exception.
var approverHeader = header + ",Company Rule";
var approverCells = new[] { "CNS01", "Approver" + tag, "Example", "2026/07/01", company + " - " + companyName,
    "A - Active", "N/A", "", "", "A001 - Administrator", "OPS - Operations", "", "4 - ESS_APPR - ESS Approvers" };
var approverCsv = approverHeader + "\n" + string.Join(',', approverCells);
var approverPreview = reader.Read(approverCsv, firstDate);
var approverHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(approverCsv)));
var oldApproverPreview = new EmployeeImportPreview([approverPreview.Rows[0] with { ValidationStatus = "Invalid" }],
    [new() { SourceRowNumber = 2, SourceField = "ID Number", RuleCode = "MissingIdentity", Message = "Earlier missing identity rule", Severity = "Error" }]);
var oldApprover = await imports.StageAsync("approver.csv", approverHash, firstDate, "database-checks", oldApproverPreview);
var approver = await Stage(approverCsv, firstDate);
Check(approver.ImportBatchId == oldApprover.ImportBatchId && approver.Status == "Staged", "An existing ESS approver review batch revalidates under the approved identity exception");
await imports.PublishAsync(approver.ImportBatchId);
var approverPerson = await sql.ExecuteScalarAsync<long>("SELECT e.PersonId FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company AND e.EmployeeCode='CNS01'", new { company });
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Person WHERE PersonId=@approverPerson AND NationalId IS NULL AND PassportNumber IS NULL", new { approverPerson }) == 1,
    "A document-free ESS approver receives an internal PersonId without an invented identity");
var approverLater = await Stage(approverCsv, firstDate.AddMonths(1));
await imports.PublishAsync(approverLater.ImportBatchId);
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company AND e.EmployeeCode='CNS01' AND e.PersonId=@approverPerson", new { company, approverPerson }) == 1,
    "Repeated document-free approver exports reuse the same person and engagement");
var anotherApprover = await Stage(approverCsv.Replace("CNS01", "CNS02"), firstDate.AddMonths(1));
await imports.PublishAsync(anotherApprover.ImportBatchId);
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT e.PersonId) FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company AND e.EmployeeCode IN ('CNS01','CNS02')", new { company }) == 2,
    "Identical names on different undocumented approver engagements do not merge people");
var ordinaryMissingCsv = approverCsv.Replace("CNS01", "CNSBAD").Replace("4 - ESS_APPR - ESS Approvers", "1 - MP - Monthly Permanent");
var ordinaryMissing = reader.Read(ordinaryMissingCsv, firstDate);
var forgedValid = new EmployeeImportPreview([ordinaryMissing.Rows[0] with { ValidationStatus = "Valid" }], []);
var ordinaryBatch = await imports.StageAsync("ordinary-no-id.csv", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ordinaryMissingCsv))), firstDate, "database-checks", forgedValid);
await Rejected(ordinaryBatch.ImportBatchId, "no reliable person identifier", "SQL publication enforces the missing-ID exception even if client validation is bypassed");
var incompletePassport = JsonSerializer.Deserialize<EmployeeImportRecord>(approverPreview.Rows[0].NormalizedDataJson)!;
incompletePassport.EmployeeCode = "CNSPASS"; incompletePassport.PassportNumber = "NO-COUNTRY-" + tag;
var passportPayload = new EmployeeImportPreview([approverPreview.Rows[0] with { NormalizedDataJson = JsonSerializer.Serialize(incompletePassport) }], []);
var passportBatch = await imports.StageAsync("approver-incomplete-passport.csv", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("incomplete-passport-" + tag))), firstDate, "database-checks", passportPayload);
await Rejected(passportBatch.ImportBatchId, "no reliable person identifier", "The approver exception does not publish a provided passport with no issuing country");
var verifiedApprover = await Stage(approverCsv.Replace(",N/A,", ",VERIFIED-" + tag + ","), new DateTime(2026, 9, 15));
await imports.PublishAsync(verifiedApprover.ImportBatchId);
Check(await sql.ExecuteScalarAsync<string>("SELECT NationalId FROM hr.Person WHERE PersonId=@approverPerson", new { approverPerson }) == "VERIFIED-" + tag,
    "A later verified identifier enriches the same ESS approver person for that engagement");
var endedApproverCells = (string[])approverCells.Clone(); endedApproverCells[5] = "T - Terminated"; endedApproverCells[7] = "2026/09/30";
var endedApprover = await Stage(approverHeader + "\n" + string.Join(',', endedApproverCells), new DateTime(2026, 9, 30));
await imports.PublishAsync(endedApprover.ImportBatchId);
var recycledApproverCells = (string[])approverCells.Clone(); recycledApproverCells[3] = "2026/10/01";
var recycledApprover = await Stage(approverHeader + "\n" + string.Join(',', recycledApproverCells), new DateTime(2026, 10, 31));
await imports.PublishAsync(recycledApprover.ImportBatchId);
Check(await sql.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT e.PersonId) FROM hr.Employment e JOIN hr.Company c ON c.CompanyId=e.CompanyId WHERE c.Code=@company AND e.EmployeeCode='CNS01'", new { company }) == 2,
    "A recycled undocumented approver code gets a separate PersonId for its new engagement");
Console.WriteLine("Database behaviour checks completed. Drop the disposable database after review.");
return 0;
