using System.Globalization;
using System.Text.Json;
using ClassLibrary.ImportAndStaging.Models;
using Microsoft.VisualBasic.FileIO;

namespace ClassLibrary.ImportAndStaging.Services;

/// <summary>Reads Sage CSV without depending on Blazor, SQL Server or Excel automation.</summary>
public sealed class EmployeeCsvReader
{
    public const int MaxRows = 20000;
    private static readonly string[] RequiredHeaders = ["Employee Code", "First Name", "Last Name", "Date Engaged", "Company", "Employee Status"];

    public EmployeeImportPreview Read(string contents, DateTime asOfDate, string delimiter = ",")
    {
        if (delimiter is not ("," or ";")) throw new ArgumentException("Choose comma or semicolon.", nameof(delimiter));
        using var parser = new TextFieldParser(new StringReader(contents));
        parser.SetDelimiters(delimiter);
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;
        string[] headers;
        try { headers = parser.ReadFields() ?? []; }
        catch (MalformedLineException ex) { throw new InvalidDataException("The CSV header is malformed.", ex); }
        headers = headers.Select(h => h.Trim().TrimStart('\uFEFF')).ToArray();
        if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length)
            throw new InvalidDataException("The CSV contains duplicate column names.");
        var missing = RequiredHeaders.Except(headers, StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length > 0) throw new InvalidDataException("Missing columns: " + string.Join(", ", missing));

        var rows = new List<PreparedEmployeeRow>();
        var issues = new List<ImportIssue>();
        var engagementKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!parser.EndOfData)
        {
            if (rows.Count >= MaxRows) throw new InvalidDataException($"The CSV exceeds {MaxRows} employee rows.");
            var line = checked((int)parser.LineNumber);
            string[] values;
            try { values = parser.ReadFields() ?? []; }
            catch (MalformedLineException ex) { throw new InvalidDataException($"Malformed CSV near row {line}.", ex); }
            if (values.Length != headers.Length) throw new InvalidDataException($"Row {line} does not match the column count.");
            var raw = headers.Zip(values).ToDictionary(p => p.First, p => p.Second, StringComparer.OrdinalIgnoreCase);
            string? Get(string name) => raw.TryGetValue(name, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
            var before = issues.Count;
            void Issue(string field, string code, string message) => issues.Add(new() { SourceRowNumber = line, SourceField = field, RuleCode = code, Message = message, Severity = "Error" });
            DateTime? Date(string name)
            {
                var v = Get(name);
                if (v is null) return null;
                if (DateTime.TryParseExact(v, ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) return result.Date;
                Issue(name, "InvalidDate", "Use dd/MM/yyyy or yyyy-MM-dd. Ambiguous dates are not guessed.");
                return null;
            }
            var record = new EmployeeImportRecord
            {
                EmployeeCode = Get("Employee Code"), NationalId = Get("ID Number"),
                PassportNumber = Get("Passport No"), PassportCountryCode = Get("Passport Country Code")?.ToUpperInvariant(),
                FirstName = Get("First Name"), LastName = Get("Last Name"), DisplayName = Get("Display Name"),
                SourceEntityCode = Get("Entity Code"), DateEngaged = Date("Date Engaged"),
                TerminationDate = Date("Termination Date"), DateOfBirth = Date("Date of Birth"),
                TransferDate = Get("Transfer Date") is not null ? Date("Transfer Date") : Date("Transfare Date"),
                SourceReportsToCode = Split(Get("Reports To Employee")).Code,
                GenderCode = Split(Get("Gender")).Code, RacialGroupCode = Split(Get("Racial Group")).Code,
                Disability = Get("Disability")
            };
            void Pair(string header, Action<string?, string?> set) { var p = Split(Get(header)); set(p.Code, p.Name); }
            Pair("Company", (c,n) => { record.CompanyCode=c; record.CompanyName=n; });
            Pair("COST_CENTRE - Cost Centre", (c,n) => { record.CostCentreCode=c; record.CostCentreName=n; });
            Pair("DEPARTMENT - Department", (c,n) => { record.DepartmentCode=c; record.DepartmentName=n; });
            Pair("Job Title", (c,n) => { record.JobTitleCode=c; record.JobTitleName=n; });
            Pair("Primary Position", (c,n) => { record.PositionCode=c; record.PositionName=n; });
            Pair("Employee Status", (c,n) => { record.EmployeeStatusCode=c; record.EmployeeStatusName=n; });
            Pair("Employment Type", (c,n) => { record.EmploymentTypeCode=c; record.EmploymentTypeName=n; });
            Pair("Company Rule", (c,n) => { record.CompanyRuleCode=c; record.CompanyRuleName=n; });
            Pair("Remuneration Definition Header", (c,n) => { record.RemunerationDefinitionCode=c; record.RemunerationDefinitionName=n; });
            Pair("Termination Reason", (c,n) => { record.TerminationReasonCode=c; record.TerminationReasonName=n; });
            foreach (var (field, value) in new[] { ("Employee Code", record.EmployeeCode), ("First Name", record.FirstName), ("Last Name", record.LastName), ("Company", record.CompanyCode), ("Employee Status", record.EmployeeStatusCode) })
                if (value is null) Issue(field, "Required", "A required value is missing.");
            if (record.DateEngaged is null) Issue("Date Engaged", "Required", "An engagement date is required.");
            if (record.NationalId is null && record.PassportNumber is null)
                Issue("ID Number", "MissingIdentity", "Supply an ID number or a passport and country; names alone cannot match a person.");
            if (record.NationalId is not null && new[] { "UNKNOWN", "N/A", "NA", "NONE", "NULL", "NOT PROVIDED", "0", "0000000000000" }.Contains(record.NationalId, StringComparer.OrdinalIgnoreCase))
                Issue("ID Number", "InvalidIdentity", "A placeholder cannot identify a person; provide a valid source identifier.");
            if (record.PassportNumber is not null && (record.PassportCountryCode?.Length != 2 || !record.PassportCountryCode.All(char.IsAsciiLetter)))
                Issue("Passport Country Code", "MissingPassportCountry", "A two-letter issuing country is required when a passport is supplied.");
            if (record.TerminationDate < record.DateEngaged) Issue("Termination Date", "InvalidRange", "Termination cannot precede engagement.");
            if (record.DateOfBirth > asOfDate.Date || record.DateOfBirth > record.DateEngaged)
                Issue("Date of Birth", "InvalidRange", "Date of birth cannot follow the reporting or engagement date.");
            if ((record.EmployeeStatusCode?.Equals("A", StringComparison.OrdinalIgnoreCase) == true || record.EmployeeStatusName?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true)
                && (record.TerminationDate is not null || record.TerminationReasonCode is not null))
                Issue("Employee Status", "ConflictingStatus", "Active status with termination details requires HR review.");
            var key = $"{record.CompanyCode}|{record.EmployeeCode}|{record.DateEngaged:yyyy-MM-dd}";
            if (!engagementKeys.Add(key)) Issue("Employee Code", "DuplicateEngagement", "This engagement appears more than once in the file.");
            ValidateLengths(record, (field, max) => Issue(field, "TooLong", $"The value exceeds {max} characters."));
            rows.Add(new(line, JsonSerializer.Serialize(raw), JsonSerializer.Serialize(record), before == issues.Count ? "Valid" : "Invalid"));
        }
        if (rows.Count == 0) throw new InvalidDataException("The CSV contains no employee rows.");
        return new(rows, issues);
    }

    private static (string? Code, string? Name) Split(string? value)
    {
        if (value is null) return (null, null);
        var at = value.IndexOf(" - ", StringComparison.Ordinal);
        if (at < 0 && value.StartsWith("- ", StringComparison.Ordinal)) return (null, value[2..].Trim());
        if (at < 0) return (value, value);
        var code = value[..at].Trim();
        var name = value[(at + 3)..].Trim();
        return (string.IsNullOrWhiteSpace(code) ? null : code, string.IsNullOrWhiteSpace(name) ? null : name);
    }

    private static void ValidateLengths(EmployeeImportRecord r, Action<string, int> issue)
    {
        // Limits mirror the import contract and SQL columns; never truncate source identifiers.
        if (r.EmployeeCode?.Length > 30) issue(nameof(r.EmployeeCode), 30);
        if (r.NationalId?.Length > 30) issue(nameof(r.NationalId), 30);
        if (r.PassportNumber?.Length > 50) issue(nameof(r.PassportNumber), 50);
        if (r.PassportCountryCode?.Length > 2) issue(nameof(r.PassportCountryCode), 2);
        if (r.FirstName?.Length > 100) issue(nameof(r.FirstName), 100);
        if (r.LastName?.Length > 100) issue(nameof(r.LastName), 100);
        if (r.DisplayName?.Length > 200) issue(nameof(r.DisplayName), 200);
        if (r.SourceEntityCode?.Length > 50) issue(nameof(r.SourceEntityCode), 50);
        if (r.CompanyCode?.Length > 30) issue(nameof(r.CompanyCode), 30);
        if (r.CompanyName?.Length > 200) issue(nameof(r.CompanyName), 200);
        if (r.DepartmentCode?.Length > 30) issue(nameof(r.DepartmentCode), 30);
        if (r.DepartmentName?.Length > 200) issue(nameof(r.DepartmentName), 200);
        if (r.CostCentreCode?.Length > 30) issue(nameof(r.CostCentreCode), 30);
        if (r.CostCentreName?.Length > 200) issue(nameof(r.CostCentreName), 200);
        if (r.JobTitleCode?.Length > 30) issue(nameof(r.JobTitleCode), 30);
        if (r.JobTitleName?.Length > 200) issue(nameof(r.JobTitleName), 200);
        if (r.PositionCode?.Length > 30) issue(nameof(r.PositionCode), 30);
        if (r.PositionName?.Length > 200) issue(nameof(r.PositionName), 200);
        if (r.EmployeeStatusCode?.Length > 30) issue(nameof(r.EmployeeStatusCode), 30);
        if (r.EmployeeStatusName?.Length > 200) issue(nameof(r.EmployeeStatusName), 200);
        if (r.EmploymentTypeCode?.Length > 30) issue(nameof(r.EmploymentTypeCode), 30);
        if (r.EmploymentTypeName?.Length > 200) issue(nameof(r.EmploymentTypeName), 200);
        if (r.CompanyRuleCode?.Length > 30) issue(nameof(r.CompanyRuleCode), 30);
        if (r.CompanyRuleName?.Length > 200) issue(nameof(r.CompanyRuleName), 200);
        if (r.RemunerationDefinitionCode?.Length > 50) issue(nameof(r.RemunerationDefinitionCode), 50);
        if (r.RemunerationDefinitionName?.Length > 200) issue(nameof(r.RemunerationDefinitionName), 200);
        if (r.TerminationReasonCode?.Length > 30) issue(nameof(r.TerminationReasonCode), 30);
        if (r.TerminationReasonName?.Length > 200) issue(nameof(r.TerminationReasonName), 200);
        if (r.SourceReportsToCode?.Length > 30) issue(nameof(r.SourceReportsToCode), 30);
        if (r.GenderCode?.Length > 30) issue(nameof(r.GenderCode), 30);
        if (r.RacialGroupCode?.Length > 30) issue(nameof(r.RacialGroupCode), 30);
        if (r.Disability?.Length > 100) issue(nameof(r.Disability), 100);
    }
}
