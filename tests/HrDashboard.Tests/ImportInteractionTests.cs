using System.Security.Claims;
using Bunit;
using ClassLibrary.DataAccess;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Services;
using HrDashboard.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using UploadPage = HrDashboard.Components.Pages.Imports.Employees;
using ReviewPage = HrDashboard.Components.Pages.Imports.Batch;

namespace HrDashboard.Tests;

public sealed class ImportInteractionTests : BunitContext
{
    private const string ValidCsv = "Employee Code,First Name,Last Name,Date Engaged,Company,Employee Status,ID Number\nEX002,Alex,Example,01/02/2026,001 - Company,A - Active,TEST-PERSON-001";
    public ImportInteractionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<IHostEnvironment>(new TestHrEnvironment());
        Services.AddScoped<HrErrorReporter>();
        Services.AddSingleton<IEmployeeImportRepository, HrAccessTests.FixtureImportRepository>();
        Services.AddSingleton<EmployeeCsvReader>();
        Services.AddSingleton<EmployeeImportService>();
        Services.AddSingleton<AuthenticationStateProvider, TestAuthentication>();
    }

    [Theory]
    [InlineData(",", "01/02/2026")]
    [InlineData(";", "2026/07/01")]
    public void SelectingAndValidatingACsvOpensItsReviewPage(string delimiter, string engagementDate)
    {
        var page = Render<UploadPage>();
        Assert.True(ValidateButton(page).HasAttribute("disabled"));
        page.Find("#delimiter").Change(delimiter);
        var csv = ValidCsv.Replace("01/02/2026", engagementDate).Replace(",", delimiter);
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(csv, "employees.csv"));
        Assert.False(ValidateButton(page).HasAttribute("disabled"));
        ValidateButton(page).Click();
        page.WaitForAssertion(() => Assert.EndsWith("imports/employees/41", Services.GetRequiredService<NavigationManager>().Uri));
    }

    [Fact]
    public void AnInvalidCsvShowsTheErrorAndDoesNotNavigateToPublication()
    {
        var page = Render<UploadPage>();
        var originalUri = Services.GetRequiredService<NavigationManager>().Uri;
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("Employee Code\nEX002", "bad.csv"));
        ValidateButton(page).Click();
        page.WaitForAssertion(() => Assert.Contains("Missing columns", page.Find("[role=alert]").TextContent));
        Assert.Equal(originalUri, Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void PublishingAValidatedBatchReplacesTheActionWithTheEmployeeLink()
    {
        var page = Render<ReviewPage>(parameters => parameters.Add(p => p.BatchId, 41));
        page.FindAll("button").Single(b => b.TextContent.Contains("Publish 1 record")).Click();
        page.WaitForAssertion(() => Assert.Contains("Import published successfully", page.Markup));
        Assert.Single(page.FindAll("a"), a => a.GetAttribute("href") == "employees?asOf=2026-07-31");
        Assert.DoesNotContain(page.FindAll("button"), b => b.TextContent.Contains("Publish 1 record"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Not a connection string")]
    public void ConnectionSetupFailureDoesNotBreakFileSelectionOrLeaveValidationBusy(string? connectionString)
    {
        Services.AddSingleton<IEmployeeImportRepository>(new EmployeeImportRepository(new SqlConnectionFactory(connectionString)));
        var page = Render<UploadPage>();
        Assert.Contains("Load recent imports", page.Find("[role=alert]").TextContent);
        Assert.Contains("HR-CONNECTION-CONFIG", page.Markup);
        var originalUri = Services.GetRequiredService<NavigationManager>().Uri;

        page.Find("#delimiter").Change(";");
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(ValidCsv.Replace(',', ';'), "employees.csv"));
        Assert.False(ValidateButton(page).HasAttribute("disabled"));
        ValidateButton(page).Click();

        page.WaitForAssertion(() => Assert.Contains("Validate and stage employee file", page.Find("[role=alert]").TextContent));
        Assert.Contains("HR-CONNECTION-CONFIG", page.Markup);
        Assert.False(ValidateButton(page).HasAttribute("disabled"));
        Assert.Equal(originalUri, Services.GetRequiredService<NavigationManager>().Uri);
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(ValidCsv.Replace(',', ';'), "retry.csv"));
        Assert.Contains("retry.csv", page.Markup);
        Assert.False(ValidateButton(page).HasAttribute("disabled"));
    }

    private static AngleSharp.Dom.IElement ValidateButton(IRenderedComponent<UploadPage> page)
        => page.FindAll("button").Single(b => b.TextContent.Contains("Validate file"));

    private sealed class TestAuthentication : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Role, "HRAdmin")], "Test"))));
    }
}
