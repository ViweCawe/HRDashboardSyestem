using System.Net;
using Xunit;

namespace HrDashboard.Tests;

public sealed class ImportPageTests
{
    [Fact] public async Task ValidBatchShowsAPreviewAndPublicationAction()
    {
        var html = await Get("/imports/employees/41", "HRAdmin");
        Assert.Contains("Alex Example", html); Assert.Contains("EX002", html);
        Assert.Contains("Publish 1 record", html); Assert.Contains("31 Jul 2026", html);
    }
    [Fact] public async Task InvalidBatchShowsIssuesAndCannotOfferPublication()
    {
        var html = await Get("/imports/employees/42", "HRAdmin");
        Assert.Contains("Supply a reliable person identifier.", html);
        Assert.Contains("Upload corrected file", html); Assert.DoesNotContain("Publish 1 record", html);
    }
    [Fact] public async Task PublishedBatchLinksToEmployeesAtItsSourceDate()
    {
        var html = await Get("/imports/employees/43", "HRAdmin");
        Assert.Contains("employees?asOf=2026-07-31", html); Assert.DoesNotContain("Publish 1 record", html);
    }
    [Fact] public async Task UnknownBatchShowsAUsefulEmptyState()
    {
        Assert.Contains("Import batch not found", await Get("/imports/employees/999", "HRAdmin"));
    }
    [Fact] public async Task PreviewEncodesUntrustedEmployeeNames()
    {
        var html = await Get("/imports/employees/44", "HRAdmin");
        Assert.Contains("&lt;img", html); Assert.DoesNotContain("<img src=x", html);
    }
    [Theory] [InlineData(null)] [InlineData("OrdinaryUser")]
    public async Task OverviewNeverShowsHrMetricsToAnUnauthorisedAccount(string? role)
    {
        var html = await Get("/", role);
        Assert.DoesNotContain("Separate employment engagements", html);
        Assert.Contains("Sign in to get started", html);
    }
    [Fact] public async Task ImportWorkspaceStartsWithAnExplicitValidationStep()
    {
        var html = await Get("/imports/employees", "HRAdmin");
        Assert.Contains("Validate file", html); Assert.Contains("employee-file", html);
        Assert.Contains("Coming next", html); Assert.DoesNotContain("Publish 1 record", html);
    }
    [Theory]
    [InlineData("/Account/Login", "Sign in to your workspace")]
    [InlineData("/Account/Register", "Create your account")]
    public async Task AccountPagesRenderWithoutHrAccess(string route, string heading)
    {
        Assert.Contains(heading, await Get(route, null));
    }
    [Fact] public async Task ReadersSeePersistedWorkspaceSummaryWithoutImportActions()
    {
        var html = await Get("/", "HRReader");
        Assert.Contains("Separate employment engagements", html);
        Assert.DoesNotContain("Import employee data", html);
    }
    [Fact] public async Task SampleDownloadIsAvailableAndContainsOnlySyntheticRows()
    {
        var csv = await Get("/examples/employees.csv", null);
        Assert.Contains("DEMO-PERSON-001", csv);
        Assert.Contains("Alex,Example", csv);
    }
    private static async Task<string> Get(string path, string? role)
    {
        await using var factory = new HrAccessTests.HrTestApplication();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        if (role is not null) client.DefaultRequestHeaders.Add("X-Test-Role", role);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
}
