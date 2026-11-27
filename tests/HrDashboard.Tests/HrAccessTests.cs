using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
    public async Task HrPagesEnforceTheirPolicies(string path, string? role, HttpStatusCode expected)
    {
        await using var factory = new HrTestApplication();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        if (role is not null) client.DefaultRequestHeaders.Add("X-Test-Role", role);
        var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK) Assert.Contains("No employee records", await response.Content.ReadAsStringAsync());
    }

    private sealed class HrTestApplication : WebApplicationFactory<Program>
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
                services.AddScoped<IEmployeeRepository, EmptyEmployeeRepository>();
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
    private sealed class EmptyEmployeeRepository : IEmployeeRepository
    {
        public Task<IReadOnlyList<EmployeeListItem>> GetAsync(DateTime asOfDate, string? search = null, int offset = 0, int pageSize = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmployeeListItem>>([]);
        public Task<EmployeeOverview?> GetOverviewAsync(long employmentId, CancellationToken cancellationToken = default)
            => Task.FromResult<EmployeeOverview?>(null);
    }
}
