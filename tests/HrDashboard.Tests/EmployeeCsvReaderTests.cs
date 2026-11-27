using System.Text.Json;
using ClassLibrary.ImportAndStaging.Models;
using ClassLibrary.ImportAndStaging.Services;
using Xunit;

namespace HrDashboard.Tests;

public sealed class EmployeeCsvReaderTests
{
    private readonly EmployeeCsvReader reader = new();
    private static readonly DateTime AsOf = new(2026, 7, 31);
    private const string Header = "Employee Code,First Name,Last Name,Date Engaged,Company,Employee Status,ID Number";
    private const string ValidRow = "C001,Alex,Example,01/02/2020,001 - Example Company,A - Active,TEST-ID-001";

    [Fact] public void ReadsEngagementDateUsingDayMonthYear()
    {
        var preview = Read(ValidRow);
        Assert.Empty(preview.Issues);
        Assert.Equal(new DateTime(2020, 2, 1), Data(preview).DateEngaged);
        Assert.Equal("001", Data(preview).CompanyCode);
    }
    [Fact] public void ReadsYearFirstSlashDateAsJulyFirstWithoutChangingTheSourceValue()
    {
        var preview = Read(ValidRow.Replace("01/02/2020", "2026/07/01"));
        Assert.Empty(preview.Issues);
        Assert.Equal(new DateTime(2026, 7, 1), Data(preview).DateEngaged);
        Assert.Contains("2026/07/01", preview.Rows[0].RawDataJson);
    }
    [Theory]
    [InlineData("Termination Date", "TerminationDate", "2026/07/01")]
    [InlineData("Date of Birth", "DateOfBirth", "1990/07/01")]
    [InlineData("Transfer Date", "TransferDate", "2026/07/01")]
    [InlineData("Transfare Date", "TransferDate", "2026/07/01")]
    public void OptionalDateFieldsAlsoAcceptYearFirstSlashDates(string header, string property, string value)
    {
        var preview = reader.Read(Header + "," + header + "\n" + ValidRow.Replace("A - Active", "T - Terminated") + "," + value, AsOf);
        Assert.Empty(preview.Issues);
        using var json = JsonDocument.Parse(preview.Rows[0].NormalizedDataJson);
        Assert.Equal(value.Replace('/', '-'), json.RootElement.GetProperty(property).GetDateTime().ToString("yyyy-MM-dd"));
    }
    [Theory]
    [InlineData("2026/31/07")]
    [InlineData("2026/02/30")]
    public void InvalidYearFirstDatesStillRequireReview(string value)
    {
        var issue = Assert.Single(Read(ValidRow.Replace("01/02/2020", value)).Issues);
        Assert.Equal("InvalidDate", issue.RuleCode);
    }
    [Fact] public void DoesNotRequireDateOfBirthOrInferEmploymentType()
    {
        var preview = Read(ValidRow);
        Assert.Empty(preview.Issues);
        Assert.Null(Data(preview).DateOfBirth);
        Assert.Null(Data(preview).EmploymentTypeCode);
    }
    [Fact] public void ReappointmentCanUseSameIdentityWithNewEmployeeCode()
    {
        var preview = Read("C001,Alex,Example,01/02/2020,001 - Company,T - Terminated,TEST-ID-001\nC002,Alex,Example,01/06/2026,001 - Company,A - Active,TEST-ID-001");
        Assert.Empty(preview.Issues);
        Assert.Equal(2, preview.Rows.Count);
    }
    [Fact] public void DetectsDuplicateEngagementInsteadOfSilentlyDiscardingIt()
    {
        var preview = Read(ValidRow + "\n" + ValidRow);
        Assert.Contains(preview.Issues, i => i.RuleCode == "DuplicateEngagement" && i.SourceRowNumber == 3);
    }
    [Fact] public void MissingIdentityRequiresReview()
    {
        var preview = Read(ValidRow.Replace("TEST-ID-001", ""));
        Assert.Contains(preview.Issues, i => i.RuleCode == "MissingIdentity");
    }
    [Fact] public void PassportOnlyNeedsIssuingCountry()
    {
        var csv = Header + ",Passport No,Passport Country Code\n" + ValidRow.Replace("TEST-ID-001", "") + ",PASSPORT-001,ZA";
        Assert.Empty(reader.Read(csv, AsOf).Issues);
        var noCountry = reader.Read(csv[..^2], AsOf);
        Assert.Contains(noCountry.Issues, i => i.RuleCode == "MissingPassportCountry");
    }
    [Theory]
    [InlineData("30/06/2026")]
    [InlineData("31/07/2026")]
    [InlineData("30/09/2026")]
    public void ActiveSourceStatusKeepsTheEndDateAsAPlanUntilSageChangesStatus(string endDate)
    {
        var preview = reader.Read(Header + ",Termination Date,Termination Reason\n" + ValidRow + "," + endDate + ",END - Contract end", AsOf);
        Assert.Empty(preview.Issues);
        var record = Data(preview);
        Assert.Equal("A", record.EmployeeStatusCode);
        Assert.NotNull(record.PlannedEndDate);
        Assert.Null(record.TerminationDate);
        Assert.Null(record.EmploymentTypeCode);
        Assert.Contains(endDate, preview.Rows[0].RawDataJson);
    }
    [Fact] public void ASourceTerminationRetainsTheActualDate()
    {
        var preview = reader.Read(Header + ",Termination Date\n" + ValidRow.Replace("A - Active", "T - Terminated") + ",2026/06/30", AsOf);
        Assert.Empty(preview.Issues);
        Assert.Equal(new DateTime(2026, 6, 30), Data(preview).TerminationDate);
        Assert.Null(Data(preview).PlannedEndDate);
    }
    [Fact] public void PlannedEndCannotPrecedeEngagement()
    {
        var preview = reader.Read(Header + ",Termination Date\n" + ValidRow + ",31/01/2020", AsOf);
        Assert.Contains(preview.Issues, i => i.RuleCode == "InvalidRange");
    }
    [Theory]
    [InlineData("N/A")]
    [InlineData(" n/a ")]
    public void UnusedPassportNaDoesNotRequireACountryWhenIdIsSupplied(string unused)
    {
        var preview = reader.Read(Header + ",Passport No,Passport Country Code\n" + ValidRow + "," + unused + ",N/A", AsOf);
        Assert.Empty(preview.Issues);
        Assert.Equal("TEST-ID-001", Data(preview).NationalId);
        Assert.Null(Data(preview).PassportNumber);
        Assert.Null(Data(preview).PassportCountryCode);
        Assert.Contains(unused.Trim(), preview.Rows[0].RawDataJson);
    }
    [Fact] public void UnusedIdNaAllowsMatchingByARealPassportAndCountry()
    {
        var preview = reader.Read(Header + ",Passport No,Passport Country Code\n" + ValidRow.Replace("TEST-ID-001", "N/A") + ",PASSPORT-001,ZA", AsOf);
        Assert.Empty(preview.Issues);
        Assert.Null(Data(preview).NationalId);
        Assert.Equal("PASSPORT-001", Data(preview).PassportNumber);
        Assert.Equal("ZA", Data(preview).PassportCountryCode);
    }
    [Fact] public void BothIdentityFieldsNaStillRequiresAReliableIdentifier()
    {
        var preview = reader.Read(Header + ",Passport No\n" + ValidRow.Replace("TEST-ID-001", "N/A") + ",N/A", AsOf);
        Assert.Equal("MissingIdentity", Assert.Single(preview.Issues).RuleCode);
    }
    [Fact] public void AnActualPassportStillNeedsItsIssuingCountry()
    {
        var preview = reader.Read(Header + ",Passport No,Passport Country Code\n" + ValidRow.Replace("TEST-ID-001", "N/A") + ",PASSPORT-001,N/A", AsOf);
        Assert.Equal("MissingPassportCountry", Assert.Single(preview.Issues).RuleCode);
    }
    [Fact] public void DoesNotGuessUsDateFormat()
    {
        Assert.Contains(Read(ValidRow.Replace("01/02/2020", "07/31/2020")).Issues, i => i.RuleCode == "InvalidDate");
    }
    [Theory]
    [InlineData("31/02/2020")]
    [InlineData("not-a-date")]
    [InlineData("07/31/2020")]
    public void AProvidedInvalidEngagementDateIsNotAlsoReportedAsMissing(string value)
    {
        var preview = Read(ValidRow.Replace("01/02/2020", value));
        var issue = Assert.Single(preview.Issues, i => i.SourceField == "Date Engaged");
        Assert.Equal("InvalidDate", issue.RuleCode);
        Assert.Equal("Invalid", preview.Rows[0].ValidationStatus);
        Assert.Null(Data(preview).DateEngaged);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankEngagementDateHasOnlyTheRequiredIssue(string value)
    {
        var preview = Read(ValidRow.Replace("01/02/2020", value));
        var issue = Assert.Single(preview.Issues, i => i.SourceField == "Date Engaged");
        Assert.Equal("Required", issue.RuleCode);
        Assert.Equal("Invalid", preview.Rows[0].ValidationStatus);
    }
    [Fact] public void TerminationCannotPrecedeEngagement()
    {
        var preview = reader.Read(Header + ",Termination Date\n" + ValidRow.Replace("A - Active", "T - Terminated") + ",31/01/2020", AsOf);
        Assert.Contains(preview.Issues, i => i.RuleCode == "InvalidRange");
    }
    [Fact] public void QuotedCommasAndExtraSourceColumnsArePreserved()
    {
        var csv = Header + ",Display Name,Source Note\n" + ValidRow + ",\"Example, Alex\",Keep this";
        var preview = reader.Read(csv, AsOf);
        Assert.Empty(preview.Issues);
        Assert.Equal("Example, Alex", Data(preview).DisplayName);
        Assert.Contains("Keep this", preview.Rows[0].RawDataJson);
    }
    [Fact] public void SupportsExcelUtf8BomAndSemicolonDelimiter()
    {
        var preview = reader.Read("\uFEFF" + Header.Replace(',', ';') + "\n" + ValidRow.Replace(',', ';'), AsOf, ";");
        Assert.Empty(preview.Issues);
    }
    [Fact] public void RejectsMissingHeaders()
    {
        Assert.Throws<InvalidDataException>(() => reader.Read("Employee Code\nC001", AsOf));
    }
    [Fact] public void RejectsDuplicateHeaders()
    {
        Assert.Throws<InvalidDataException>(() => reader.Read(Header + ",Company\n" + ValidRow + ",001", AsOf));
    }
    [Fact] public void RejectsEmptyAndMalformedFiles()
    {
        Assert.Throws<InvalidDataException>(() => reader.Read(Header, AsOf));
        Assert.Throws<InvalidDataException>(() => reader.Read(Header + "\nC001,Alex", AsOf));
    }
    [Fact] public void DoesNotTruncateLongEmployeeCodes()
    {
        var preview = Read(ValidRow.Replace("C001", new string('X', 31)));
        Assert.Contains(preview.Issues, i => i.RuleCode == "TooLong" && i.SourceField == "EmployeeCode");
    }
    [Fact] public void DoesNotTreatEmptySplitCodeAsAValidCompany()
    {
        Assert.Contains(Read(ValidRow.Replace("001 - Example Company", " - Company")).Issues, i => i.SourceField == "Company");
    }
    [Fact] public void PlaceholderIdentityIsNotUsedToMergeUnknownPeople()
    {
        Assert.Contains(Read(ValidRow.Replace("TEST-ID-001", "UNKNOWN")).Issues, i => i.RuleCode == "InvalidIdentity");
    }
    [Fact] public void PassportCountryMustContainTwoLetters()
    {
        var csv = Header + ",Passport No,Passport Country Code\n" + ValidRow.Replace("TEST-ID-001", "") + ",PASS-001,12";
        Assert.Contains(reader.Read(csv, AsOf).Issues, i => i.RuleCode == "MissingPassportCountry");
    }
    private EmployeeImportPreview Read(string rows) => reader.Read(Header + "\n" + rows, AsOf);
    private static EmployeeImportRecord Data(EmployeeImportPreview preview) => JsonSerializer.Deserialize<EmployeeImportRecord>(preview.Rows[0].NormalizedDataJson)!;
}
