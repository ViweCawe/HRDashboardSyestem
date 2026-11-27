using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Models;
using ClassLibrary.PeoplAndEmployment.Data;
using ClassLibrary.PeoplAndEmployment.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace HrDashboard.Tests;

public sealed class HrAccessTests
{
    [Theory]
    [InlineData("/employees", null, HttpStatusCode.Unauthorized)]
    [InlineData("/employees/1", null, HttpStatusCode.Unauthorized)]
    [InlineData("/imports/employees", null, HttpStatusCode.Unauthorized)]
    [InlineData("/employees", "OrdinaryUser", HttpStatusCode.Forbidden)]
    [InlineData("/imports/employees", "HRReader", HttpStatusCode.Forbidden)]
    [InlineData("/employees", "HRReader", HttpStatusCode.OK)]
    [InlineData("/employees", "HRAdmin", HttpStatusCode.OK)]
    [InlineData("/employees/1", "OrdinaryUser", HttpStatusCode.Forbidden)]
    [InlineData("/imports/employees", "OrdinaryUser", HttpStatusCode.Forbidden)]
    [InlineData("/employees/1", "HRReader", HttpStatusCode.OK)]
    [InlineData("/imports/employees", "HRAdmin", HttpStatusCode.OK)]
    [InlineData("/imports/employees/41", null, HttpStatusCode.Unauthorized)]
    [InlineData("/imports/employees/41", "HRReader", HttpStatusCode.Forbidden)]
    [InlineData("/imports/employees/41", "OrdinaryUser", HttpStatusCode.Forbidden)]
    public async Task HrPagesEnforceTheirPolicies(string path, string? role, HttpStatusCode expected)
    {
        await using var factory = new HrTestApplication();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        if (role is not null) client.DefaultRequestHeaders.Add("X-Test-Role", role);
        var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var expectedContent = path switch
            {
                "/imports/employees" => "Import centre",
                "/employees/1" => "Employee engagement not found.",
                _ => "All employment engagements"
            };
            Assert.Contains(expectedContent, await response.Content.ReadAsStringAsync());
        }
    }

    internal sealed class HrTestApplication : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                });
                services.AddScoped<IEmployeeRepository, FixtureEmployeeRepository>();
                services.AddSingleton<IEmployeeImportRepository, FixtureImportRepository>();
            });
        }
    }
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].ToString();
            if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Name, "Test User"), new Claim(ClaimTypes.Role, role)], "Test");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
        }
    }
    internal sealed class FixtureImportRepository : IEmployeeImportRepository
    {
        private readonly HashSet<long> published = [];
        public Task<IReadOnlyList<ImportBatch>> GetRecentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ImportBatch>>([
            new() { ImportBatchId = 41, FileName = "employee-export.csv", SourceAsOfDate = new(2026, 7, 31), RowCount = 1, Status = published.Contains(41) ? "Published" : "Staged" },
            new() { ImportBatchId = 42, FileName = "employees-to-review.csv", SourceAsOfDate = new(2026, 7, 31), RowCount = 1, Status = "Review" },
            new() { ImportBatchId = 43, FileName = "employees-previous.csv", SourceAsOfDate = new(2026, 6, 30), RowCount = 1, Status = "Published" }
        ]);
        public Task<IReadOnlyList<ImportIssue>> GetIssuesAsync(long batchId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ImportIssue>>(batchId == 42 ? [new() { SourceRowNumber = 2, SourceField = "ID Number", Message = "Supply a reliable person identifier.", Severity = "Error" }] : []);
        public Task<ImportBatch> StageAsync(string fileName, string fileHash, DateTime asOfDate, string importedBy, EmployeeImportPreview preview, CancellationToken cancellationToken = default)
            => Task.FromResult(new ImportBatch { ImportBatchId = preview.Issues.Count > 0 ? 42 : 41, Status = preview.Issues.Count > 0 ? "Review" : "Staged" });
        public Task<EmployeeImportBatchSummary?> GetBatchAsync(long batchId, CancellationToken cancellationToken = default) => Task.FromResult(batchId is 41 or 42 or 43 or 44 ? new EmployeeImportBatchSummary
        { ImportBatchId = batchId, FileName = "employee-export.csv", SourceAsOfDate = new DateTime(2026, 7, 31), Status = Status(batchId), RowCount = 1, ValidRowCount = batchId == 42 ? 0 : 1, InvalidRowCount = batchId == 42 ? 1 : 0, IssueCount = batchId == 42 ? 1 : 0, PublishedRowCount = Status(batchId) == "Published" ? 1 : 0 } : null);
        public Task<IReadOnlyList<EmployeeImportPreviewRow>> GetRowsAsync(long batchId, int offset = 0, int pageSize = 50, string? status = null, CancellationToken cancellationToken = default)
        {
            var rowStatus = batchId == 42 ? "Invalid" : Status(batchId) == "Published" ? "Published" : "Valid";
            if (offset > 0 || status is not null && status != rowStatus) return Task.FromResult<IReadOnlyList<EmployeeImportPreviewRow>>([]);
            return Task.FromResult<IReadOnlyList<EmployeeImportPreviewRow>>([new() { SourceRowNumber = 2, EmployeeCode = "EX002", DisplayName = batchId == 44 ? "<img src=x onerror=alert(1)>" : "Alex Example", DateEngagedText = "01/02/2026", CompanyName = "Demonstration Company", DepartmentName = "Operations", JobTitleName = "Supervisor", ValidationStatus = rowStatus }]);
        }
        public Task<ImportDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ImportDashboardSummary { EmployeeRecords = 2, PublishedImports = 1, NeedsReview = 1, ReadyToPublish = 1, LatestSnapshotDate = new(2026, 7, 31) });
        public Task PublishAsync(long batchId, CancellationToken cancellationToken = default) { published.Add(batchId); return Task.CompletedTask; }
        private string Status(long id) => id == 42 ? "Review" : id == 43 || published.Contains(id) ? "Published" : "Staged";
    }

    private sealed class FixtureEmployeeRepository : IEmployeeRepository
    {
        private static EmployeeListItem Employee => new() { PersonId = 1, EmploymentId = 101, EmployeeCode = "EX002", DisplayName = "Alex Example", CompanyName = "Demonstration Company", DateEngaged = new(2026, 2, 1), PlannedEndDate = new(2026, 12, 31), SnapshotDate = new(2026, 7, 31), StatusName = "Active", DepartmentName = "Operations", JobTitleName = "Supervisor", EmploymentTypeName = "Permanent" };
        public Task<IReadOnlyList<EmployeeListItem>> GetAsync(DateTime asOfDate, string? search = null, int offset = 0, int pageSize = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmployeeListItem>>([Employee, new() { PersonId = 2, EmploymentId = 102, EmployeeCode = "EX001", DisplayName = "Jordan Sample", CompanyName = "Demonstration Company", DateEngaged = new(2026, 3, 1), SnapshotDate = new(2026, 7, 31), StatusName = "Active", DepartmentName = "Retail", JobTitleName = "Administrator", EmploymentTypeName = "Contract" }]);
        public Task<EmployeeOverview?> GetOverviewAsync(long employmentId, CancellationToken cancellationToken = default)
            => Task.FromResult<EmployeeOverview?>(employmentId != 101 ? null : new(Employee,
                [Employee, new() { EmploymentId = 100, EmployeeCode = "EX001", DateEngaged = new(2020, 2, 1), StatusName = "Terminated" }],
                [new() { SnapshotDate = new(2026, 7, 31), PlannedEndDate = new(2026, 12, 31), StatusName = "Active", DepartmentName = "Operations", CostCentreName = "Operations", JobTitleName = "Supervisor", EmploymentTypeName = "Permanent" }, new() { SnapshotDate = new(2026, 6, 30), StatusName = "Active", DepartmentName = "Operations", CostCentreName = "Operations", JobTitleName = "Administrator", EmploymentTypeName = "Permanent" }],
                [new() { FieldName = "Job title", OldValue = "Administrator", NewValue = "Supervisor", ChangeType = "DetectedChange", IsConfirmed = false }]));
    }
}
