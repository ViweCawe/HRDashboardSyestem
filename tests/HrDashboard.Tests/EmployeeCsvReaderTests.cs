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
    [Fact] public void ActiveStatusAndTerminationDetailsRequireReview()
    {
        var preview = reader.Read(Header + ",Termination Date,Termination Reason\n" + ValidRow + ",30/06/2026,DS - Discharged", AsOf);
        Assert.Contains(preview.Issues, i => i.RuleCode == "ConflictingStatus");
    }
    [Fact] public void DoesNotGuessUsDateFormat()
    {
        Assert.Contains(Read(ValidRow.Replace("01/02/2020", "07/31/2020")).Issues, i => i.RuleCode == "InvalidDate");
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
